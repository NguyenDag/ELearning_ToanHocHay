using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IAIHintRepository
    {
        Task<AIHint> CreateAsync(AIHint hint);
        Task<AIHint?> GetByIdAsync(int hintId);
        Task<IEnumerable<AIHint>> GetByAttemptAsync(int attemptId);
        Task<IEnumerable<AIHint>> GetByAttemptAndQuestionAsync(int attemptId, int questionId);
        Task<AIHint?> UpdateAsync(AIHint hint);
        Task<bool> DeleteAsync(int hintId);
    }
}
