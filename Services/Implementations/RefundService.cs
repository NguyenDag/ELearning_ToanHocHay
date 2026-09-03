using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class RefundService : IRefundService
    {
        private readonly AppDbContext _context;
        private readonly ISystemConfigService _config;
        private readonly IResourceAccessService _access;
        private readonly IRefundFieldProtector _protector;
        private readonly IRefundEventWriter _events;
        private readonly ILogger<RefundService> _logger;

        // SystemConfig fallbacks
        private const decimal DefaultDailyCapVnd = 20_000_000m;
        private const int DefaultMaxRequestsPerUserPer30d = 3;
        private const int DefaultMaxPaymentAgeDays = 180;
        private const decimal DefaultDualControlThresholdVnd = 0m;
        private const int DefaultTimezoneOffsetHours = 7;
        private const int DefaultStaleDisbursedDays = 3;

        private static readonly RefundRequestStatus[] NonTerminalForPayment =
        {
            RefundRequestStatus.PendingReview, RefundRequestStatus.PendingSecondApproval,
            RefundRequestStatus.Approved, RefundRequestStatus.Batched,
            RefundRequestStatus.Disbursed, RefundRequestStatus.Failed
        };

        private static readonly RefundRequestStatus[] CountsTowardDailyCap =
        {
            RefundRequestStatus.Approved, RefundRequestStatus.Batched,
            RefundRequestStatus.Disbursed, RefundRequestStatus.Completed
        };

        public RefundService(
            AppDbContext context,
            ISystemConfigService config,
            IResourceAccessService access,
            IRefundFieldProtector protector,
            IRefundEventWriter events,
            ILogger<RefundService> logger)
        {
            _context = context;
            _config = config;
            _access = access;
            _protector = protector;
            _events = events;
            _logger = logger;
        }

        // ---------------------------------------------------------------- create

        public async Task<ApiResponse<RefundRequestDto>> CreateAsync(CreateRefundRequestDto dto, ClaimsPrincipal actor)
        {
            var actorUserId = actor.GetUserId();
            if (actorUserId == null)
                return ApiResponse<RefundRequestDto>.Forbidden("Token không hợp lệ");

            var isFinance = actor.HasUserType(UserType.FinanceManager, UserType.SystemAdmin);

            var payment = await _context.Payments
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.PaymentId == dto.PaymentId);
            if (payment == null)
                return ApiResponse<RefundRequestDto>.NotFound("Không tìm thấy giao dịch");

            if (!isFinance && !await _access.CanAccessPaymentAsync(actor, dto.PaymentId))
                return ApiResponse<RefundRequestDto>.Forbidden("Bạn không thể yêu cầu hoàn tiền cho giao dịch này");

            if (payment.Status is not (PaymentStatus.Completed or PaymentStatus.PartiallyRefunded))
                return ApiResponse<RefundRequestDto>.ErrorResponse("Chỉ hoàn tiền được giao dịch đã Completed");

            var maxAgeDays = await _config.GetIntAsync("refund.maxPaymentAgeDays", DefaultMaxPaymentAgeDays);
            if (payment.PaymentDate < DateTime.UtcNow.AddDays(-maxAgeDays))
                return ApiResponse<RefundRequestDto>.ErrorResponse(
                    $"Giao dịch quá {maxAgeDays} ngày, không hoàn tự động được — liên hệ hỗ trợ");

            var alreadyRefunded = payment.RefundAmount ?? 0m;
            var remaining = payment.Amount - alreadyRefunded;
            var amount = dto.Amount ?? remaining;
            if (amount <= 0 || amount > remaining)
                return ApiResponse<RefundRequestDto>.ErrorResponse(
                    $"Số tiền hoàn không hợp lệ (còn có thể hoàn tối đa {remaining:N0} VND)");

            var hasOpen = await _context.RefundRequests.AnyAsync(r =>
                r.PaymentId == dto.PaymentId && NonTerminalForPayment.Contains(r.Status));
            if (hasOpen)
                return ApiResponse<RefundRequestDto>.Conflict(
                    "Giao dịch này đã có một yêu cầu hoàn tiền đang xử lý");

            var beneficiaryUserId = payment.PaidByUserId;

            // Rate-limit theo người thụ hưởng (SystemAdmin được bỏ qua).
            if (!actor.IsSystemAdmin())
            {
                var maxPer30d = await _config.GetIntAsync(
                    "refund.maxRequestsPerUserPer30d", DefaultMaxRequestsPerUserPer30d);
                var since = DateTime.UtcNow.AddDays(-30);
                var recentCount = await _context.RefundRequests.CountAsync(r =>
                    r.BeneficiaryUserId == beneficiaryUserId
                    && r.CreatedAt >= since
                    && r.Status != RefundRequestStatus.Rejected
                    && r.Status != RefundRequestStatus.Cancelled);
                if (recentCount >= maxPer30d)
                    return ApiResponse<RefundRequestDto>.Conflict(
                        $"Đã đạt giới hạn {maxPer30d} yêu cầu hoàn tiền trong 30 ngày. Liên hệ hỗ trợ.");
            }

            var now = DateTime.UtcNow;
            var request = new RefundRequest
            {
                PublicId = Guid.NewGuid(),
                PaymentId = dto.PaymentId,
                RequestedByUserId = actorUserId.Value,
                OnBehalf = isFinance,
                BeneficiaryUserId = beneficiaryUserId,
                ReasonCode = dto.ReasonCode,
                ReasonNote = dto.ReasonNote,
                Amount = amount,
                Status = RefundRequestStatus.PendingReview,
                BankBin = dto.BankBin.Trim(),
                BankAccountNumberProtected = _protector.Protect(dto.BankAccountNumber.Trim()),
                BankAccountNumberLast4 = _protector.Last4(dto.BankAccountNumber),
                BankAccountHolderName = dto.BankAccountHolderName.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.RefundRequests.Add(request);
            await _context.SaveChangesAsync();

            _events.Add(_context, RefundEventType.Created, request.RefundRequestId,
                toStatus: nameof(RefundRequestStatus.PendingReview), amount: amount,
                note: $"Reason={dto.ReasonCode}; onBehalf={isFinance}");
            await NotifyFinanceAsync("Yêu cầu hoàn tiền mới",
                $"Yêu cầu hoàn {amount:N0} VND cho giao dịch #{dto.PaymentId} đang chờ duyệt.");
            await _context.SaveChangesAsync();

            return ApiResponse<RefundRequestDto>.Created(MapDto(request), "Đã tạo yêu cầu hoàn tiền");
        }

        // ---------------------------------------------------------------- reads

        public async Task<ApiResponse<PagedResult<RefundRequestDto>>> GetMineAsync(int userId, PagedRequest request)
        {
            var page = await _context.RefundRequests.AsNoTracking()
                .Where(r => r.BeneficiaryUserId == userId || r.RequestedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToPagedResultAsync(request);
            return ApiResponse<PagedResult<RefundRequestDto>>.SuccessResponse(page.Map(MapDto));
        }

        public async Task<ApiResponse<RefundRequestDetailDto>> GetByIdAsync(int id, ClaimsPrincipal actor)
        {
            var request = await _context.RefundRequests.AsNoTracking()
                .Include(r => r.Events)
                .FirstOrDefaultAsync(r => r.RefundRequestId == id);
            if (request == null)
                return ApiResponse<RefundRequestDetailDto>.NotFound("Không tìm thấy yêu cầu hoàn tiền");

            var uid = actor.GetUserId();
            var isFinance = actor.HasUserType(UserType.FinanceManager, UserType.SystemAdmin);
            if (!isFinance && request.BeneficiaryUserId != uid && request.RequestedByUserId != uid)
                return ApiResponse<RefundRequestDetailDto>.Forbidden();

            var detail = new RefundRequestDetailDto();
            CopyInto(request, detail);
            detail.Events = request.Events
                .OrderBy(e => e.CreatedAt)
                .Select(e => new RefundEventDto
                {
                    EventType = e.EventType,
                    FromStatus = e.FromStatus,
                    ToStatus = e.ToStatus,
                    ActorUserId = e.ActorUserId,
                    ActorUserType = e.ActorUserType,
                    AmountSnapshot = e.AmountSnapshot,
                    Note = e.Note,
                    CorrelationId = e.CorrelationId,
                    CreatedAt = e.CreatedAt
                })
                .ToList();
            return ApiResponse<RefundRequestDetailDto>.SuccessResponse(detail);
        }

        public async Task<ApiResponse<PagedResult<RefundRequestDto>>> ListAsync(
            PagedRequest request, RefundRequestStatus? status)
        {
            var q = _context.RefundRequests.AsNoTracking().AsQueryable();
            if (status.HasValue) q = q.Where(r => r.Status == status.Value);

            var page = await q.OrderByDescending(r => r.CreatedAt).ToPagedResultAsync(request);
            return ApiResponse<PagedResult<RefundRequestDto>>.SuccessResponse(page.Map(MapDto));
        }

        // ---------------------------------------------------------------- approve / reject / cancel

        public async Task<ApiResponse<RefundRequestDto>> ApproveAsync(int id, ApproveRefundDto dto, ClaimsPrincipal actor)
        {
            var actorId = actor.GetUserId()!.Value;
            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestId == id);
            if (request == null)
                return ApiResponse<RefundRequestDto>.NotFound("Không tìm thấy yêu cầu hoàn tiền");

            if (request.Status is not (RefundRequestStatus.PendingReview or RefundRequestStatus.PendingSecondApproval))
                return ApiResponse<RefundRequestDto>.Conflict(
                    $"Không thể duyệt yêu cầu ở trạng thái {request.Status}");

            var threshold = await _config.GetDecimalAsync(
                "refund.dualControlThresholdVnd", DefaultDualControlThresholdVnd);
            var needsDual = threshold > 0 && request.Amount >= threshold;
            var now = DateTime.UtcNow;

            // First approval of a dual-control request — no cap consumption yet.
            if (needsDual && request.Status == RefundRequestStatus.PendingReview)
            {
                var from = request.Status.ToString();
                request.FirstApprovedByUserId = actorId;
                request.FirstApprovedAt = now;
                request.Status = RefundRequestStatus.PendingSecondApproval;
                request.UpdatedAt = now;
                _events.Add(_context, RefundEventType.Approved, id, fromStatus: from,
                    toStatus: request.Status.ToString(), amount: request.Amount,
                    note: $"Duyệt lần 1 (dual-control, ngưỡng {threshold:N0}). {dto.Note}".Trim());
                await _context.SaveChangesAsync();
                return ApiResponse<RefundRequestDto>.SuccessResponse(MapDto(request),
                    "Đã duyệt lần 1 — chờ người thứ hai duyệt");
            }

            if (needsDual && request.Status == RefundRequestStatus.PendingSecondApproval
                && request.FirstApprovedByUserId == actorId)
                return ApiResponse<RefundRequestDto>.Conflict(
                    "Cùng một người không thể duyệt cả hai lần (dual-control)");

            var capError = await CheckDailyCapAsync(request.Amount);
            if (capError != null)
                return ApiResponse<RefundRequestDto>.ErrorResponse(capError);

            var fromStatus = request.Status.ToString();
            var isSecond = request.Status == RefundRequestStatus.PendingSecondApproval;
            request.ApprovedByUserId = actorId;
            request.ApprovedAt = now;
            request.FirstApprovedByUserId ??= actorId;
            request.Status = RefundRequestStatus.Approved;
            request.UpdatedAt = now;
            _events.Add(_context, isSecond ? RefundEventType.SecondApproved : RefundEventType.Approved, id,
                fromStatus: fromStatus, toStatus: request.Status.ToString(), amount: request.Amount, note: dto.Note);
            await _context.SaveChangesAsync();

            return ApiResponse<RefundRequestDto>.SuccessResponse(MapDto(request), "Đã duyệt yêu cầu hoàn tiền");
        }

        public async Task<ApiResponse<RefundRequestDto>> RejectAsync(int id, RejectRefundDto dto, ClaimsPrincipal actor)
        {
            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestId == id);
            if (request == null)
                return ApiResponse<RefundRequestDto>.NotFound("Không tìm thấy yêu cầu hoàn tiền");
            if (request.Status is not (RefundRequestStatus.PendingReview or RefundRequestStatus.PendingSecondApproval))
                return ApiResponse<RefundRequestDto>.Conflict($"Không thể từ chối yêu cầu ở trạng thái {request.Status}");

            var from = request.Status.ToString();
            var now = DateTime.UtcNow;
            request.Status = RefundRequestStatus.Rejected;
            request.RejectedByUserId = actor.GetUserId();
            request.RejectedAt = now;
            request.RejectionReason = dto.Reason;
            request.UpdatedAt = now;
            _events.Add(_context, RefundEventType.Rejected, id, fromStatus: from,
                toStatus: request.Status.ToString(), amount: request.Amount, note: dto.Reason);
            await _context.SaveChangesAsync();
            return ApiResponse<RefundRequestDto>.SuccessResponse(MapDto(request), "Đã từ chối yêu cầu");
        }

        public async Task<ApiResponse<RefundRequestDto>> CancelAsync(int id, CancelRefundDto dto, ClaimsPrincipal actor)
        {
            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestId == id);
            if (request == null)
                return ApiResponse<RefundRequestDto>.NotFound("Không tìm thấy yêu cầu hoàn tiền");

            var cancellable = request.Status is RefundRequestStatus.PendingReview
                or RefundRequestStatus.PendingSecondApproval
                or RefundRequestStatus.Approved
                or RefundRequestStatus.Failed;
            if (!cancellable || request.RefundBatchId != null)
                return ApiResponse<RefundRequestDto>.Conflict(
                    "Chỉ huỷ được yêu cầu chưa vào lô chi hộ (huỷ lô nếu đã gộp)");

            var from = request.Status.ToString();
            var now = DateTime.UtcNow;
            request.Status = RefundRequestStatus.Cancelled;
            request.UpdatedAt = now;
            _events.Add(_context, RefundEventType.Cancelled, id, fromStatus: from,
                toStatus: request.Status.ToString(), amount: request.Amount, note: dto.Reason);
            await _context.SaveChangesAsync();
            return ApiResponse<RefundRequestDto>.SuccessResponse(MapDto(request), "Đã huỷ yêu cầu");
        }

        // ---------------------------------------------------------------- confirm / fail / retry

        public async Task<ApiResponse<RefundRequestDto>> ConfirmAsync(int id, ConfirmRefundDto dto, ClaimsPrincipal actor)
        {
            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestId == id);
            if (request == null)
                return ApiResponse<RefundRequestDto>.NotFound("Không tìm thấy yêu cầu hoàn tiền");
            if (request.Status is not (RefundRequestStatus.Approved or RefundRequestStatus.Disbursed))
                return ApiResponse<RefundRequestDto>.Conflict(
                    $"Chỉ xác nhận đã chuyển tiền cho yêu cầu Approved / Disbursed (hiện {request.Status})");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var from = request.Status.ToString();
            var now = DateTime.UtcNow;
            request.BankTransactionRef = dto.BankTransactionRef.Trim();
            request.Status = RefundRequestStatus.Completed;
            request.CompletedAt = now;
            request.UpdatedAt = now;

            await RefundCompletion.ApplyAsync(_context, request);

            _events.Add(_context, RefundEventType.Confirmed, id, fromStatus: from,
                toStatus: request.Status.ToString(), amount: request.Amount,
                note: $"bankRef={dto.BankTransactionRef}. {dto.Note}".Trim());

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return ApiResponse<RefundRequestDto>.SuccessResponse(MapDto(request), "Đã xác nhận hoàn tiền");
        }

        public async Task<ApiResponse<RefundRequestDto>> MarkFailedAsync(
            int id, MarkRefundFailedDto dto, ClaimsPrincipal actor)
        {
            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestId == id);
            if (request == null)
                return ApiResponse<RefundRequestDto>.NotFound("Không tìm thấy yêu cầu hoàn tiền");
            if (request.Status is not (RefundRequestStatus.Approved or RefundRequestStatus.Disbursed))
                return ApiResponse<RefundRequestDto>.Conflict(
                    $"Chỉ đánh dấu thất bại cho yêu cầu Approved / Disbursed (hiện {request.Status})");

            var from = request.Status.ToString();
            var now = DateTime.UtcNow;
            request.Status = RefundRequestStatus.Failed;
            request.FailedAt = now;
            request.FailureReason = dto.Reason;
            request.UpdatedAt = now;
            _events.Add(_context, RefundEventType.MarkedFailed, id, fromStatus: from,
                toStatus: request.Status.ToString(), amount: request.Amount, note: dto.Reason);
            await _context.SaveChangesAsync();
            return ApiResponse<RefundRequestDto>.SuccessResponse(MapDto(request), "Đã đánh dấu thất bại");
        }

        public async Task<ApiResponse<RefundRequestDto>> RetryAsync(int id, ClaimsPrincipal actor)
        {
            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.RefundRequestId == id);
            if (request == null)
                return ApiResponse<RefundRequestDto>.NotFound("Không tìm thấy yêu cầu hoàn tiền");
            if (request.Status != RefundRequestStatus.Failed)
                return ApiResponse<RefundRequestDto>.Conflict("Chỉ thử lại yêu cầu đang ở trạng thái Failed");

            var capError = await CheckDailyCapAsync(request.Amount);
            if (capError != null)
                return ApiResponse<RefundRequestDto>.ErrorResponse(capError);

            var now = DateTime.UtcNow;
            request.Status = RefundRequestStatus.Approved;
            request.ApprovedByUserId = actor.GetUserId();
            request.ApprovedAt = now;
            request.RefundBatchId = null;
            request.FailedAt = null;
            request.FailureReason = null;
            request.UpdatedAt = now;
            _events.Add(_context, RefundEventType.RetryQueued, id, fromStatus: nameof(RefundRequestStatus.Failed),
                toStatus: request.Status.ToString(), amount: request.Amount);
            await _context.SaveChangesAsync();
            return ApiResponse<RefundRequestDto>.SuccessResponse(MapDto(request), "Đã đưa lại vào hàng chờ chi");
        }

        // ---------------------------------------------------------------- usage / reconciliation / sweep

        public async Task<ApiResponse<RefundDailyUsageDto>> GetDailyUsageAsync()
        {
            var (start, reset) = await DayWindowAsync();
            var cap = await _config.GetDecimalAsync("refund.dailyCapVnd", DefaultDailyCapVnd);
            var used = await _context.RefundRequests
                .Where(r => CountsTowardDailyCap.Contains(r.Status) && r.ApprovedAt >= start)
                .SumAsync(r => (decimal?)r.Amount) ?? 0m;

            return ApiResponse<RefundDailyUsageDto>.SuccessResponse(new RefundDailyUsageDto
            {
                CapVnd = cap,
                UsedVnd = used,
                RemainingVnd = Math.Max(0, cap - used),
                WindowStartUtc = start,
                ResetAtUtc = reset
            });
        }

        public async Task<RefundReconciliationReport> BuildReconciliationAsync()
        {
            var staleDays = await _config.GetIntAsync("refund.staleDisbursedDays", DefaultStaleDisbursedDays);
            var staleCutoff = DateTime.UtcNow.AddDays(-staleDays);

            var pendingReview = await _context.RefundRequests.CountAsync(r =>
                r.Status == RefundRequestStatus.PendingReview
                || r.Status == RefundRequestStatus.PendingSecondApproval);

            var approvedNotBatched = await _context.RefundRequests.CountAsync(r =>
                r.Status == RefundRequestStatus.Approved && r.RefundBatchId == null);

            var disbursedNotCompleted = await _context.RefundRequests.CountAsync(r =>
                r.Status == RefundRequestStatus.Disbursed);

            var staleDisbursed = await _context.RefundRequests.CountAsync(r =>
                r.Status == RefundRequestStatus.Disbursed && r.UpdatedAt < staleCutoff);

            var batchesAwaiting = await _context.RefundBatches.CountAsync(b =>
                b.Status == RefundBatchStatus.Exported);

            var completedTotal = await _context.RefundRequests
                .Where(r => r.Status == RefundRequestStatus.Completed)
                .SumAsync(r => (decimal?)r.Amount) ?? 0m;

            var paymentIdsWithCompletedRefund = await _context.RefundRequests
                .Where(r => r.Status == RefundRequestStatus.Completed)
                .Select(r => r.PaymentId).Distinct().ToListAsync();
            var paymentRefundedTotal = await _context.Payments
                .Where(p => paymentIdsWithCompletedRefund.Contains(p.PaymentId))
                .SumAsync(p => (decimal?)p.RefundAmount) ?? 0m;

            return new RefundReconciliationReport(
                PendingReviewCount: pendingReview,
                ApprovedNotBatchedCount: approvedNotBatched,
                DisbursedNotCompletedCount: disbursedNotCompleted,
                StaleDisbursedCount: staleDisbursed,
                BatchesAwaitingDisbursementCount: batchesAwaiting,
                CompletedRefundTotal: completedTotal,
                PaymentRefundedTotal: paymentRefundedTotal,
                Balanced: completedTotal == paymentRefundedTotal);
        }

        public async Task<int> RunStaleSweepAsync()
        {
            var staleDays = await _config.GetIntAsync("refund.staleDisbursedDays", DefaultStaleDisbursedDays);
            var cutoff = DateTime.UtcNow.AddDays(-staleDays);

            var stale = await _context.RefundRequests
                .Where(r => r.Status == RefundRequestStatus.Disbursed && r.UpdatedAt < cutoff)
                .Select(r => new { r.RefundRequestId, r.Amount })
                .ToListAsync();
            if (stale.Count == 0) return 0;

            var notified = DateTime.UtcNow.AddDays(-1);
            var already = await _context.Notifications.AnyAsync(n =>
                n.Audience == NotifyAudience.Staff
                && n.Title == "Hoàn tiền quá hạn xác nhận"
                && n.CreatedAt > notified);
            if (already) return 0;

            await NotifyFinanceAsync("Hoàn tiền quá hạn xác nhận",
                $"{stale.Count} yêu cầu hoàn tiền đã Disbursed quá {staleDays} ngày mà chưa xác nhận hoàn tất " +
                $"(tổng {stale.Sum(s => s.Amount):N0} VND). Kiểm tra internet banking và confirm.");
            await _context.SaveChangesAsync();
            _logger.LogWarning("Refund stale sweep: {Count} disbursed requests overdue confirmation", stale.Count);
            return stale.Count;
        }

        // ---------------------------------------------------------------- helpers

        private async Task<string?> CheckDailyCapAsync(decimal amount)
        {
            var (start, _) = await DayWindowAsync();
            var cap = await _config.GetDecimalAsync("refund.dailyCapVnd", DefaultDailyCapVnd);
            var used = await _context.RefundRequests
                .Where(r => CountsTowardDailyCap.Contains(r.Status) && r.ApprovedAt >= start)
                .SumAsync(r => (decimal?)r.Amount) ?? 0m;

            if (used + amount > cap)
                return $"Vượt trần hoàn tiền trong ngày ({used:N0}/{cap:N0} VND đã dùng, " +
                       $"yêu cầu {amount:N0} VND). Duyệt lại sau 00:00 giờ VN.";
            return null;
        }

        private async Task<(DateTime start, DateTime reset)> DayWindowAsync()
        {
            var offsetHours = await _config.GetIntAsync("refund.timezoneOffsetHours", DefaultTimezoneOffsetHours);
            var offset = TimeSpan.FromHours(offsetHours);
            var localMidnight = (DateTime.UtcNow + offset).Date;
            var startUtc = DateTime.SpecifyKind(localMidnight - offset, DateTimeKind.Utc);
            return (startUtc, startUtc.AddDays(1));
        }

        private async Task NotifyFinanceAsync(string title, string message)
        {
            var financeUserIds = await _context.Users
                .Where(u => (u.UserType == UserType.FinanceManager || u.UserType == UserType.SystemAdmin) && u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var uid in financeUserIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = uid,
                    StudentId = null,
                    Audience = NotifyAudience.Staff,
                    Title = title,
                    Message = message,
                    NotificationType = NotificationType.Info,
                    CreatedAt = now
                });
            }
        }

        private RefundRequestDto MapDto(RefundRequest r)
        {
            var dto = new RefundRequestDto();
            CopyInto(r, dto);
            return dto;
        }

        private static void CopyInto(RefundRequest r, RefundRequestDto dto)
        {
            dto.RefundRequestId = r.RefundRequestId;
            dto.PublicId = r.PublicId;
            dto.PaymentId = r.PaymentId;
            dto.BeneficiaryUserId = r.BeneficiaryUserId;
            dto.OnBehalf = r.OnBehalf;
            dto.ReasonCode = r.ReasonCode;
            dto.ReasonNote = r.ReasonNote;
            dto.Amount = r.Amount;
            dto.Status = r.Status;
            dto.BankBin = r.BankBin;
            dto.BankAccountNumberLast4 = r.BankAccountNumberLast4;
            dto.BankAccountHolderName = r.BankAccountHolderName;
            dto.ApprovedByUserId = r.ApprovedByUserId;
            dto.ApprovedAt = r.ApprovedAt;
            dto.RefundBatchId = r.RefundBatchId;
            dto.BankTransactionRef = r.BankTransactionRef;
            dto.RejectionReason = r.RejectionReason;
            dto.FailureReason = r.FailureReason;
            dto.CompletedAt = r.CompletedAt;
            dto.CreatedAt = r.CreatedAt;
        }
    }
}
