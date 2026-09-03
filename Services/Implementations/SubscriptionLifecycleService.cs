using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SubscriptionLifecycleService : ISubscriptionLifecycleService
    {
        private readonly AppDbContext _context;
        private readonly SePayOptions _options;
        private readonly ILogger<SubscriptionLifecycleService> _logger;

        public SubscriptionLifecycleService(
            AppDbContext context, IOptions<SePayOptions> options, ILogger<SubscriptionLifecycleService> logger)
        {
            _context = context;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<LifecycleSweepResult> RunSweepAsync()
        {
            var now = DateTime.UtcNow;

            // 1. Active past EndDate -> Expired
            var expired = await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= now)
                .ToListAsync();
            foreach (var s in expired) s.Status = SubscriptionStatus.Expired;

            // 2. Pending older than the timeout -> Cancelled, its pending payment -> Failed
            var cutoff = now.AddMinutes(-Math.Max(1, _options.PendingTimeoutMinutes));
            var stale = await _context.Subscriptions
                .Include(s => s.Payment)
                .Where(s => s.Status == SubscriptionStatus.Pending && s.CreatedAt <= cutoff)
                .ToListAsync();
            foreach (var s in stale)
            {
                s.Status = SubscriptionStatus.Cancelled;
                if (s.Payment is { Status: PaymentStatus.Pending })
                    s.Payment.Status = PaymentStatus.Failed;
            }

            if (expired.Count > 0 || stale.Count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Lifecycle sweep: {Expired} expired, {Released} released",
                    expired.Count, stale.Count);
            }

            return new LifecycleSweepResult(expired.Count, stale.Count);
        }

        public async Task<ReconciliationReport> BuildReconciliationAsync()
        {
            var completed = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Select(p => p.Amount)
                .ToListAsync();

            var activeSubs = await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .Select(s => new { s.SubscriptionId, s.AmountPaid, PaymentStatus = (PaymentStatus?)s.Payment!.Status })
                .ToListAsync();

            var activeWithoutCompletedPayment = activeSubs.Count(s => s.PaymentStatus != PaymentStatus.Completed);

            var completedPaymentSubStatuses = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.Subscription != null)
                .Select(p => p.Subscription!.Status)
                .ToListAsync();
            var completedWithoutActiveSub = completedPaymentSubStatuses.Count(st => st != SubscriptionStatus.Active);

            return new ReconciliationReport(
                CompletedPaymentCount: completed.Count,
                CompletedPaymentTotal: completed.Sum(),
                ActiveSubscriptionCount: activeSubs.Count,
                ActiveSubscriptionAmountTotal: activeSubs.Sum(s => s.AmountPaid),
                ActiveWithoutCompletedPayment: activeWithoutCompletedPayment,
                CompletedPaymentWithoutActiveSubscription: completedWithoutActiveSub,
                Balanced: activeWithoutCompletedPayment == 0);
        }
    }

    /// <summary>Runs <see cref="ISubscriptionLifecycleService.RunSweepAsync"/> on a timer.</summary>
    public class SubscriptionLifecycleHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly SePayOptions _options;
        private readonly ILogger<SubscriptionLifecycleHostedService> _logger;

        public SubscriptionLifecycleHostedService(
            IServiceProvider services, IOptions<SePayOptions> options, ILogger<SubscriptionLifecycleHostedService> logger)
        {
            _services = services;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_options.LifecycleIntervalMinutes <= 0)
            {
                _logger.LogInformation("Subscription lifecycle sweep disabled (interval <= 0)");
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.LifecycleIntervalMinutes));
            do
            {
                try
                {
                    using var scope = _services.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ISubscriptionLifecycleService>().RunSweepAsync();
                    // P6 — piggy-back the daily inactivity notification sweep on the same timer.
                    await scope.ServiceProvider.GetRequiredService<INotificationRuleEngine>().RunInactivitySweepAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background maintenance sweep failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
