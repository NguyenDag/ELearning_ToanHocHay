using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IExerciseService
    {
        Task<ApiResponse<IEnumerable<ExerciseDto>>> GetAllAsync();
        Task<ApiResponse<ExerciseDto>> GetByIdAsync(int exerciseId);
        Task<ApiResponse<ExerciseDto>> CreateExerciseAsync(ExerciseRequestDto exercise);
        Task<ApiResponse<ExerciseDto>> UpdateExerciseAsync(int id, ExerciseRequestDto exercise);
        Task<ApiResponse<bool>> DeleteExerciseAsync(int exerciseId);
    }
}
