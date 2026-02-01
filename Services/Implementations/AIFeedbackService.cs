using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AIFeedbackService : IAIFeedbackService
    {
        private readonly IAIFeedbackRepository _feedbackRepository;
        private readonly IExerciseAttemptRepository _attemptRepository;
        private readonly IQuestionRepository _questionRepository;

        public AIFeedbackService(
            IAIFeedbackRepository feedbackRepository,
            IExerciseAttemptRepository attemptRepository,
            IQuestionRepository questionRepository)
        {
            _feedbackRepository = feedbackRepository;
            _attemptRepository = attemptRepository;
            _questionRepository = questionRepository;
        }

        public async Task<ApiResponse<AIFeedbackDto>> CreateAsync(CreateAIFeedbackDto dto)
        {
            // Check Attempt
            var attempt = await _attemptRepository.GetAttemptByIdAsync(dto.AttemptId);
            if (attempt == null)
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Attempt not found");

            // Check Question
            var question = await _questionRepository.GetQuestionByIdAsync(dto.QuestionId);
            if (question == null)
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Question not found");

            var feedback = new AIFeedback
            {
                AttemptId = dto.AttemptId,
                QuestionId = dto.QuestionId,
                FullSolution = dto.FullSolution,
                MistakeAnalysis = dto.MistakeAnalysis,
                ImprovementAdvice = dto.ImprovementAdvice
            };

            var created = await _feedbackRepository.CreateAsync(feedback);

            return ApiResponse<AIFeedbackDto>.SuccessResponse(
                MapToDto(created),
                "AI feedback created successfully"
            );
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int feedbackId)
        {
            var success = await _feedbackRepository.DeleteAsync(feedbackId);

            return success
                ? ApiResponse<bool>.SuccessResponse(true, "Feedback deleted")
                : ApiResponse<bool>.ErrorResponse("Feedback not found");
        }

        public async Task<ApiResponse<IEnumerable<AIFeedbackDto>>> GetByAttemptAsync(int attemptId)
        {
            var feedbacks = await _feedbackRepository.GetByAttemptAsync(attemptId);

            return ApiResponse<IEnumerable<AIFeedbackDto>>.SuccessResponse(
                feedbacks.Select(MapToDto)
            );
        }

        public async Task<ApiResponse<AIFeedbackDto>> GetByIdAsync(int feedbackId)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
            if (feedback == null)
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Feedback not found");

            return ApiResponse<AIFeedbackDto>.SuccessResponse(MapToDto(feedback));
        }

        public async Task<ApiResponse<AIFeedbackDto>> UpdateAsync(int feedbackId, UpdateAIFeedbackDto dto)
        {
            var existing = await _feedbackRepository.GetByIdAsync(feedbackId);
            if (existing == null)
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Feedback not found");

            existing.FullSolution = dto.FullSolution;
            existing.MistakeAnalysis = dto.MistakeAnalysis;
            existing.ImprovementAdvice = dto.ImprovementAdvice;

            var updated = await _feedbackRepository.UpdateAsync(existing);

            return ApiResponse<AIFeedbackDto>.SuccessResponse(
                MapToDto(updated!),
                "Feedback updated successfully"
            );
        }

        private static AIFeedbackDto MapToDto(AIFeedback f)
        {
            return new AIFeedbackDto
            {
                FeedbackId = f.FeedbackId,
                AttemptId = f.AttemptId,
                QuestionId = f.QuestionId,
                FullSolution = f.FullSolution,
                MistakeAnalysis = f.MistakeAnalysis,
                ImprovementAdvice = f.ImprovementAdvice,
                CreatedAt = f.CreatedAt
            };
        }
    }
}
