using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Implementations;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ExerciseService : IExerciseService
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IMapper _mapper;

        public ExerciseService(IExerciseRepository exerciseRepository, IMapper mapper)
        {
            _exerciseRepository = exerciseRepository;
            _mapper = mapper;
        }
        public async Task<ApiResponse<ExerciseDto>> CreateExerciseAsync(ExerciseRequestDto exercise)
        {
            try
            {
                var _exercise = new Exercise
                {
                    TopicId = exercise.TopicId,
                    ChapterId = exercise.ChapterId,
                    ExerciseName = exercise.ExerciseName,
                    ExerciseType = exercise.ExerciseType,
                    TotalQuestions = exercise.TotalQuestions,
                    DurationMinutes = exercise.DurationMinutes,
                    IsFree = exercise.IsFree,
                    IsActive = exercise.IsActive,
                    TotalPoints = exercise.TotalPoints,
                    PassingScore = exercise.PassingScore,
                    Status = exercise.Status,
                };
                await _exerciseRepository.CreateExerciseAsync(_exercise);
                return ApiResponse<ExerciseDto>.SuccessResponse(
                    _mapper.Map<ExerciseDto>(_exercise),
                    "Exercise created successfully"
                    );
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseDto>.ErrorResponse(
                    "Error creating exercise",
                    new List<string> { ex.Message }
                    );
            }
        }

        public async Task<ApiResponse<bool>> DeleteExerciseAsync(int exerciseId)
        {
            try
            {
                var exercise = await _exerciseRepository.GetExerciseByIdAsync(exerciseId);
                if (exercise == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Exercise not found",
                        new List<string>
                        {
                            $"No exercise found with id: {exerciseId}"
                        }
                        );
                }
                var deleted = await _exerciseRepository.DeleteExerciseAsync(exerciseId);
                return ApiResponse<bool>.SuccessResponse(deleted,
                    "Exercise deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse("Error deleting exercise",
                    new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<IEnumerable<ExerciseDto>>> GetAllAsync()
        {
            try
            {
                var _exercise = await _exerciseRepository.GetAllAsync();
                return ApiResponse<IEnumerable<ExerciseDto>>.SuccessResponse(_mapper.Map<IEnumerable<ExerciseDto>>(_exercise), "Exercises retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<ExerciseDto>>.ErrorResponse(
                    "Error retrieving users",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<ExerciseDto>> GetByIdAsync(int exerciseId)
        {
            try
            {
                var _exercise = await _exerciseRepository.GetExerciseByIdAsync(exerciseId);
                if (_exercise == null)
                    return ApiResponse<ExerciseDto>.ErrorResponse("Exercise not found", new List<string> { $"No exercise found with ID: {exerciseId}" });
                return ApiResponse<ExerciseDto>.SuccessResponse(_mapper.Map<ExerciseDto>(_exercise));
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseDto>.ErrorResponse(
                    "Error retrieving exercise",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<ExerciseDto>> UpdateExerciseAsync(int id, ExerciseRequestDto exerciseRequestDto)
        {
            try
            {
                var exercise = await _exerciseRepository.GetExerciseByIdAsync(id);
                if (exercise == null)
                    return ApiResponse<ExerciseDto>.ErrorResponse(
                            "ExerciseId not found",
                            new List<string> { $"No user found with ID: {id}" }
                        );
                // Update info
                exercise.TopicId = exerciseRequestDto.TopicId;
                exercise.ChapterId = exerciseRequestDto.ChapterId;
                exercise.ExerciseName = exerciseRequestDto.ExerciseName;
                exercise.ExerciseType = exerciseRequestDto.ExerciseType;
                exercise.TotalQuestions = exerciseRequestDto.TotalQuestions;
                exercise.DurationMinutes = exerciseRequestDto.DurationMinutes;
                exercise.IsFree = exerciseRequestDto.IsFree;
                exercise.IsActive = exerciseRequestDto.IsActive;
                exercise.TotalPoints = exerciseRequestDto.TotalPoints;
                exercise.PassingScore = exerciseRequestDto.PassingScore;
                exercise.Status = exerciseRequestDto.Status;
                await _exerciseRepository.UpdateExerciseAsync(exercise);

                return ApiResponse<ExerciseDto>.SuccessResponse(_mapper.Map<ExerciseDto>(exercise), "Exercise updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseDto>.ErrorResponse(
                    "Error updating exercise",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
