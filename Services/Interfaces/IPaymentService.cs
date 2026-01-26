using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Payment;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<IEnumerable<PaymentDto>>> GetAllAsync();
        Task<ApiResponse<PaymentDto>> GetByIdAsync(int id);
        Task<ApiResponse<PaymentDto>> CreateAsync(CreatePaymentDto dto);
        Task<ApiResponse<bool>> UpdateStatusAsync(int id, UpdatePaymentStatusDto dto);
    }
}
