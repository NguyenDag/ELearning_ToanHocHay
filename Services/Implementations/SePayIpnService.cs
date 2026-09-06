using System.Text.Json;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SePayIpnService : ISePayIpnService
    {
        private readonly AppDbContext _context;
        private readonly ISePayService _sePayService;
        private readonly SePayOptions _options;
        private readonly ILogger<SePayIpnService> _logger;

        public SePayIpnService(
            AppDbContext context,
            ISePayService sePayService,
            IOptions<SePayOptions> options,
            ILogger<SePayIpnService> logger)
        {
            _context = context;
            _sePayService = sePayService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IpnResult> ProcessAsync(SePayIpnRequest request)
        {
            var refCode = string.IsNullOrWhiteSpace(request.referenceCode)
                ? $"NOCODE-{Guid.NewGuid():N}"
                : request.referenceCode.Trim();

            // Idempotency by transaction: one log row per referenceCode.
            var log = await _context.SePayIpnLogs.FirstOrDefaultAsync(l => l.ReferenceCode == refCode);
            if (log is { Outcome: IpnOutcome.Processed })
                return new IpnResult(IpnOutcome.Duplicate, "Already processed");

            if (log == null)
            {
                log = new SePayIpnLog { ReferenceCode = refCode, CreatedAt = DateTime.UtcNow };
                _context.SePayIpnLogs.Add(log);
            }

            log.RawPayload = JsonSerializer.Serialize(request);
            log.TransferAmount = request.transferAmount;
            log.TransferType = request.transferType;

            var result = await EvaluateAsync(request, log);

            log.Outcome = result.Outcome;
            log.ResultMessage = result.Message;
            if (result.Outcome == IpnOutcome.Processed) log.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return result;
        }

        private async Task<IpnResult> EvaluateAsync(SePayIpnRequest request, SePayIpnLog log)
        {
            var subscriptionId = _sePayService.ExtractSubscriptionId(request.content);
            log.SubscriptionId = subscriptionId;

            var subscription = subscriptionId == null
                ? null
                : await _context.Subscriptions
                    .Include(s => s.Package)
                    .Include(s => s.Payment)
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId.Value);

            // Ma trận quyết định thuần (xem SePayIpnEvaluator) — phần ghi bên dưới chỉ chạy khi Processed.
            var outcome = SePayIpnEvaluator.Evaluate(request, subscription, _options.AmountToleranceVnd);
            if (outcome != IpnOutcome.Processed)
            {
                if (outcome == IpnOutcome.AmountMismatch && subscription != null)
                    _logger.LogWarning("IPN amount mismatch sub {Sub}: got {Got}, expected {Exp}",
                        subscription.SubscriptionId, request.transferAmount, (long)Math.Round(subscription.AmountPaid));
                return new IpnResult(outcome, DescribeNonProcessed(outcome));
            }

            // Processed ⇒ subscription chắc chắn khác null (SePayIpnEvaluator đã loại nhánh null).
            if (subscription == null)
                return new IpnResult(IpnOutcome.Ignored, DescribeNonProcessed(IpnOutcome.Ignored));

            await using var tx = await _context.Database.BeginTransactionAsync();

            if (subscription.Payment != null)
            {
                subscription.Payment.Status = PaymentStatus.Completed;
                subscription.Payment.TransactionId = request.referenceCode;
                subscription.Payment.PaymentDate = DateTime.UtcNow;
            }

            var durationDays = subscription.Package is { DurationDays: > 0 }
                ? subscription.Package.DurationDays
                : 30;

            subscription.Status = SubscriptionStatus.Active;
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow.AddDays(durationDays);

            // A2-11 — one Active subscription per student at a time.
            if (subscription.StudentId != null)
            {
                var others = await _context.Subscriptions
                    .Where(s => s.StudentId == subscription.StudentId
                                && s.SubscriptionId != subscription.SubscriptionId
                                && s.Status == SubscriptionStatus.Active)
                    .ToListAsync();
                foreach (var o in others) o.Status = SubscriptionStatus.Expired;
            }

            log.Outcome = IpnOutcome.Processed;
            log.ResultMessage = "Success";
            log.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return new IpnResult(IpnOutcome.Processed, "Success");
        }

        private static string DescribeNonProcessed(IpnOutcome outcome) => outcome switch
        {
            IpnOutcome.Duplicate => "Already processed",
            IpnOutcome.AmountMismatch => "Amount mismatch",
            _ => "Ignored (out transaction / invalid content / subscription not payable)"
        };
    }
}
