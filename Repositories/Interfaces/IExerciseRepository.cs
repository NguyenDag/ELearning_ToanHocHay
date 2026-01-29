using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IExerciseRepository
    {
        Task<Exercise?> GetExerciseByIdAsync(int exerciseId);
        Task<Exercise> GetExerciseWithQuestionsAsync(int exerciseId);
        Task<List<Question>> GetRandomQuestionsAsync(int? questionBankId, int count);
        Task<IEnumerable<Exercise>> GetAllAsync();
        Task<Exercise> CreateExerciseAsync(Exercise exercise);
        Task<Exercise?> UpdateExerciseAsync(Exercise exercise);
        Task<bool> DeleteExerciseAsync(int exerciseId);

        Task<bool> AddQuestionsToExerciseAsync(int exerciseId, List<int> questionIds, double scorePerQuestion);

        Task<IEnumerable<Exercise>> GetByLessonIdAsync(int lessonId);
        Task<IEnumerable<Exercise>> GetByChapterIdAsync(int chapterId);
        Task<IEnumerable<Exercise>> GetByTopicIdAsync(int topicId);

        Task<List<ExerciseQuestion>> GetExerciseQuestionsAsync(int exerciseId);
        Task<bool> RemoveQuestionFromExerciseAsync(int exerciseId, int questionId);
        Task<bool> UpdateExerciseQuestionScoreAsync(int exerciseId, int questionId, double score);
    }
}
