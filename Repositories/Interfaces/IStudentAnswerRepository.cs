using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IStudentAnswerRepository
    {
        Task<StudentAnswer> CreateAnswerAsync(StudentAnswer answer);
        
        Task CreateAsync(StudentAnswer answer);
        Task<StudentAnswer> UpdateAnswerAsync(StudentAnswer answer);
        void Update(StudentAnswer answer);

        Task<StudentAnswer> GetAnswerAsync(int attemptId, int questionId);
        Task<List<StudentAnswer>> GetAttemptAnswersAsync(int attemptId);
    }
}
