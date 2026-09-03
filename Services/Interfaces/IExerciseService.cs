using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Exercise;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IExerciseService
    {
        Task<ApiResponse<IEnumerable<ExerciseDto>>> GetAllAsync();
        Task<ApiResponse<ExerciseDetailDto>> GetByIdAsync(int exerciseId);
        Task<ApiResponse<ExerciseDto>> CreateExerciseAsync(ExerciseRequestDto exercise, int createdBy);
        Task<ApiResponse<ExerciseDto>> UpdateExerciseAsync(int id, ExerciseRequestDto exercise);
        Task<ApiResponse<bool>> DeleteExerciseAsync(int exerciseId);
        Task<ApiResponse<bool>> AddQuestionsToExerciseAsync(int exerciseId, AddQuestionsToExerciseDto dto);

        // A3/P2 — publish workflow + question listing
        Task<ApiResponse<ExerciseDto>> SetPublishedAsync(int exerciseId, bool published);
        Task<ApiResponse<ExerciseDetailDto>> GetForEditAsync(int exerciseId);
        Task<ApiResponse<List<QuestionInExerciseDto>>> GetExerciseQuestionsAsync(int exerciseId);

        Task<ApiResponse<IEnumerable<ExerciseDto>>> GetByLessonIdAsync(int lessonId);
        Task<ApiResponse<IEnumerable<ExerciseDto>>> GetByChapterIdAsync(int chapterId);
        Task<ApiResponse<IEnumerable<ExerciseDto>>> GetByTopicIdAsync(int topicId);

        //Task<ApiResponse<IEnumerable<ExerciseQuestionDto>>> GetExerciseQuestionsAsync(int exerciseId);
        Task<ApiResponse<bool>> RemoveQuestionFromExerciseAsync(int exerciseId, int questionId);
        Task<ApiResponse<bool>> UpdateExerciseQuestionScoreAsync(
            int exerciseId, int questionId, double score);
    }
}
