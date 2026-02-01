using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.AIHint;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IAIHintService
    {
        Task<ApiResponse<AIHintDto>> CreateAsync(CreateAIHintDto dto);
        Task<ApiResponse<AIHintDto>> GetByIdAsync(int hintId);
        Task<ApiResponse<IEnumerable<AIHintDto>>> GetByAttemptAsync(int attemptId);
        Task<ApiResponse<IEnumerable<AIHintDto>>> GetByAttemptAndQuestionAsync(int attemptId, int questionId);
        Task<ApiResponse<AIHintDto>> UpdateAsync(int hintId, UpdateAIHintDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int hintId);
    }
}
