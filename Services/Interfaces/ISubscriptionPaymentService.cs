using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Subscription;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ISubscriptionPaymentService
    {
        Task<ApiResponse<int>> CreatePendingAsync(CreateSubscriptionDto dto);
    }
}
