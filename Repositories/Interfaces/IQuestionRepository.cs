using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IQuestionRepository
    {
        // Hàm lấy 1 câu hỏi (kèm đáp án) theo ID
        Task<Question?> GetQuestionByIdAsync(int id);

        Task<Question> CreateAsync(Question question);
        Task<List<Question>> CreateMultipleAsync(List<Question> questions);

        // A3/P2 — question bank management
        Task<(List<Question> Items, int Total)> GetByBankAsync(
            int bankId, QuestionStatus? status, string? search, int page, int pageSize);
        Task SaveAsync();
        Task DeleteAsync(Question question);
        Task<bool> IsUsedInAttemptsAsync(int questionId);
    }
}
