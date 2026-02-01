using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.AIHint;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AIHintService : IAIHintService
    {
        private readonly IAIHintRepository _hintRepository;
        private readonly IExerciseAttemptRepository _attemptRepository;
        private readonly IQuestionRepository _questionRepository;

        public AIHintService(
            IAIHintRepository hintRepository,
            IExerciseAttemptRepository attemptRepository,
            IQuestionRepository questionRepository)
        {
            _hintRepository = hintRepository;
            _attemptRepository = attemptRepository;
            _questionRepository = questionRepository;
        }

        public async Task<ApiResponse<AIHintDto>> CreateAsync(CreateAIHintDto dto)
        {
            // Check Attempt
            var attempt = await _attemptRepository.GetAttemptByIdAsync(dto.AttemptId);
            if (attempt == null)
                return ApiResponse<AIHintDto>.ErrorResponse("Attempt not found");

            // Check Question
            var question = await _questionRepository.GetQuestionByIdAsync(dto.QuestionId);
            if (question == null)
                return ApiResponse<AIHintDto>.ErrorResponse("Question not found");

            var hint = new AIHint
            {
                AttemptId = dto.AttemptId,
                QuestionId = dto.QuestionId,
                HintText = dto.HintText,
                HintLevel = dto.HintLevel
            };

            var created = await _hintRepository.CreateAsync(hint);

            return ApiResponse<AIHintDto>.SuccessResponse(
                MapToDto(created),
                "AI hint created successfully"
            );
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int hintId)
        {
            var success = await _hintRepository.DeleteAsync(hintId);

            return success
                ? ApiResponse<bool>.SuccessResponse(true, "Hint deleted")
                : ApiResponse<bool>.ErrorResponse("Hint not found");
        }

        public async Task<ApiResponse<IEnumerable<AIHintDto>>> GetByAttemptAsync(int attemptId)
        {
            var hints = await _hintRepository.GetByAttemptAsync(attemptId);

            return ApiResponse<IEnumerable<AIHintDto>>.SuccessResponse(
                hints.Select(MapToDto)
            );
        }

        public async Task<ApiResponse<IEnumerable<AIHintDto>>> GetByAttemptAndQuestionAsync(int attemptId, int questionId)
        {
            var hints = await _hintRepository.GetByAttemptAndQuestionAsync(attemptId, questionId);

            return ApiResponse<IEnumerable<AIHintDto>>.SuccessResponse(
                hints.Select(MapToDto)
            );
        }

        public async Task<ApiResponse<AIHintDto>> GetByIdAsync(int hintId)
        {
            var hint = await _hintRepository.GetByIdAsync(hintId);
            if (hint == null)
                return ApiResponse<AIHintDto>.ErrorResponse("Hint not found");

            return ApiResponse<AIHintDto>.SuccessResponse(MapToDto(hint));
        }

        public async Task<ApiResponse<AIHintDto>> UpdateAsync(int hintId, UpdateAIHintDto dto)
        {
            var existing = await _hintRepository.GetByIdAsync(hintId);
            if (existing == null)
                return ApiResponse<AIHintDto>.ErrorResponse("Hint not found");

            existing.HintText = dto.HintText;
            existing.HintLevel = dto.HintLevel;

            var updated = await _hintRepository.UpdateAsync(existing);

            return ApiResponse<AIHintDto>.SuccessResponse(
                MapToDto(updated!),
                "Hint updated successfully"
            );
        }

        private static AIHintDto MapToDto(AIHint h)
        {
            return new AIHintDto
            {
                HintId = h.HintId,
                AttemptId = h.AttemptId,
                QuestionId = h.QuestionId,
                HintText = h.HintText,
                HintLevel = h.HintLevel,
                CreatedAt = h.CreatedAt
            };
        }
    }
}
