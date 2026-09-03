using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Payment;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<IEnumerable<PaymentDto>>> GetAllAsync();
        Task<ApiResponse<PagedResult<PaymentDto>>> GetPagedAsync(Common.PagedRequest request, Data.Entities.PaymentStatus? status);
        Task<ApiResponse<PaymentDto>> GetByIdAsync(int id);
        Task<ApiResponse<bool>> UpdateStatusAsync(int id, UpdatePaymentStatusDto dto);

        /// <summary>P5 — "my payment history" (as payer or beneficiary), paged.</summary>
        Task<ApiResponse<PagedResult<PaymentDto>>> GetMyPaymentsAsync(int userId, int page, int pageSize);
    }
}
