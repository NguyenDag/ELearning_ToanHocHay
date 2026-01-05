using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IExerciseAttemptRepository
    {
        Task<ExerciseAttempt> CreateAttemptAsync(ExerciseAttempt attempt);
        Task<ExerciseAttempt> GetAttemptByIdAsync(int attemptId);
        Task<ExerciseAttempt> GetAttemptWithDetailsAsync(int attemptId);
        Task<List<ExerciseAttempt>> GetStudentAttemptsAsync(int studentId);
        Task<ExerciseAttempt> UpdateAttemptAsync(ExerciseAttempt attempt);
        Task<bool> HasActiveAttemptAsync(int studentId, int exerciseId);
    }
}
