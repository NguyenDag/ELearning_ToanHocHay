using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IAIFeedbackService
    {
        Task<ApiResponse<AIFeedbackDto>> CreateAsync(CreateAIFeedbackDto dto);
        Task<ApiResponse<AIFeedbackDto>> GetByIdAsync(int feedbackId);
        Task<ApiResponse<IEnumerable<AIFeedbackDto>>> GetByAttemptAsync(int attemptId);
        Task<ApiResponse<AIFeedbackDto>> UpdateAsync(int feedbackId, UpdateAIFeedbackDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int feedbackId);
    }
}
