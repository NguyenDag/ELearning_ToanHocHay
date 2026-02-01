using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IAIFeedbackRepository
    {
        Task<AIFeedback> CreateAsync(AIFeedback feedback);
        Task<AIFeedback?> GetByIdAsync(int feedbackId);
        Task<IEnumerable<AIFeedback>> GetByAttemptAsync(int attemptId);
        Task<AIFeedback?> UpdateAsync(AIFeedback feedback);
        Task<bool> DeleteAsync(int feedbackId);
    }
}
