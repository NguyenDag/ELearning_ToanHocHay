using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public record RefundReconciliationReport(
        int PendingReviewCount,
        int ApprovedNotBatchedCount,
        int DisbursedNotCompletedCount,
        int StaleDisbursedCount,
        int BatchesAwaitingDisbursementCount,
        decimal CompletedRefundTotal,
        decimal PaymentRefundedTotal,
        bool Balanced);

    /// <summary>
    /// Pha 2 — hoàn tiền bán tự động: vòng đời yêu cầu (tạo → duyệt → xác nhận),
    /// trần hoàn/ngày, rate-limit theo người dùng, ghi log truy vết.
    /// </summary>
    public interface IRefundService
    {
        Task<ApiResponse<RefundRequestDto>> CreateAsync(CreateRefundRequestDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<PagedResult<RefundRequestDto>>> GetMineAsync(int userId, PagedRequest request);
        Task<ApiResponse<RefundRequestDetailDto>> GetByIdAsync(int id, ClaimsPrincipal actor);
        Task<ApiResponse<PagedResult<RefundRequestDto>>> ListAsync(PagedRequest request, RefundRequestStatus? status);

        Task<ApiResponse<RefundRequestDto>> ApproveAsync(int id, ApproveRefundDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<RefundRequestDto>> RejectAsync(int id, RejectRefundDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<RefundRequestDto>> CancelAsync(int id, CancelRefundDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<RefundRequestDto>> ConfirmAsync(int id, ConfirmRefundDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<RefundRequestDto>> MarkFailedAsync(int id, MarkRefundFailedDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<RefundRequestDto>> RetryAsync(int id, ClaimsPrincipal actor);

        Task<ApiResponse<RefundDailyUsageDto>> GetDailyUsageAsync();
        Task<RefundReconciliationReport> BuildReconciliationAsync();

        /// <summary>Nền — chỉ cảnh báo Finance về yêu cầu Disbursed quá hạn, không tự đổi trạng thái.</summary>
        Task<int> RunStaleSweepAsync();
    }
}
