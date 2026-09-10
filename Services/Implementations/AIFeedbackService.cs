using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay_Control.Models.DTOs.AI;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AIFeedbackService : IAIFeedbackService
    {
        private readonly IAIFeedbackRepository _feedbackRepository;
        private readonly IExerciseAttemptRepository _attemptRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IAIService _aiService;
        private readonly IAiQuotaService _quota;
        private readonly ILogger<AIFeedbackService> _logger;

        public AIFeedbackService(
            IAIFeedbackRepository feedbackRepository,
            IExerciseAttemptRepository attemptRepository,
            IQuestionRepository questionRepository,
            IAIService aiService,
            IAiQuotaService quota,
            ILogger<AIFeedbackService> logger)
        {
            _feedbackRepository = feedbackRepository;
            _attemptRepository = attemptRepository;
            _questionRepository = questionRepository;
            _aiService = aiService;
            _quota = quota;
            _logger = logger;
        }

        public async Task<ApiResponse<AIFeedbackDto>> CreateAsync(CreateAIFeedbackDto dto)
        {
            // Check Attempt
            var attempt = await _attemptRepository.GetAttemptWithDetailsAsync(dto.AttemptId);
            if (attempt == null)
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Không tìm thấy lượt làm bài");

            // Check Question
            var question = await _questionRepository.GetQuestionByIdAsync(dto.QuestionId);
            if (question == null)
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Không tìm thấy câu hỏi");

            string fullSolution = dto.FullSolution ?? string.Empty;
            string mistakeAnalysis = dto.MistakeAnalysis ?? string.Empty;
            string improvementAdvice = dto.ImprovementAdvice ?? string.Empty;

            // KIỂM TRA XEM ĐÃ CÓ FEEDBACK CHƯA
            var existingFeedbacks = await _feedbackRepository.GetByAttemptAsync(dto.AttemptId);
            var existing = existingFeedbacks.FirstOrDefault(f => f.QuestionId == dto.QuestionId);
            if (existing != null)
            {
                return ApiResponse<AIFeedbackDto>.SuccessResponse(MapToDto(existing), "Feedback already exists");
            }

            // Nếu dữ liệu trống, gọi AI sinh mới
            if (string.IsNullOrWhiteSpace(fullSolution))
            {
                _logger.LogInformation($"Generating AI feedback for Question {dto.QuestionId}, Attempt {dto.AttemptId}");

                // Lấy câu trả lời của học sinh cho câu hỏi này trong lượt làm bài
                var studentAnswer = attempt.StudentAnswers?.FirstOrDefault(a => a.QuestionId == dto.QuestionId);
                
                var isSkipped = string.IsNullOrWhiteSpace(dto.StudentAnswer)
                                || dto.StudentAnswer!.Equals("Bạn chưa trả lời câu hỏi này");

                var aiRequest = new AIFeedbackRequest
                {
                    QuestionText = question.QuestionText ?? string.Empty,
                    QuestionType = question.QuestionType.ToString(),
                    StudentAnswer = isSkipped ? "Học sinh chưa trả lời câu hỏi này" : dto.StudentAnswer!,
                    CorrectAnswer = question.CorrectAnswer ?? string.Empty,
                    // So sánh đúng: câu trả lời của học sinh có trùng với đáp án đúng không
                    IsCorrect = !isSkipped
                                && dto.StudentAnswer!.Trim().Equals(question.CorrectAnswer?.Trim() ?? "", StringComparison.OrdinalIgnoreCase),
                    Explanation = question.Explanation,
                    AttemptId = dto.AttemptId,
                    QuestionId = dto.QuestionId,
                    QuestionImageUrl = question.QuestionImageUrl,
                    Options = question.QuestionOptions?.Select(o => new AIOptionDto
                    {
                        OptionId = o.OptionId,
                        OptionText = o.OptionText ?? string.Empty,
                        ImageUrl = o.ImageUrl,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                };

                var aiResponse = await _aiService.GenerateFeedbackStructuredAsync(aiRequest);

                if (aiResponse != null && aiResponse.Status == "success")
                {
                    fullSolution = aiResponse.FullSolution ?? string.Empty;
                    mistakeAnalysis = aiResponse.MistakeAnalysis ?? string.Empty;
                    improvementAdvice = aiResponse.ImprovementAdvice ?? string.Empty;
                }
                else
                {
                    _logger.LogError("AI chưa tạo được nhận xét. Vui lòng thử lại sau.");
                    return ApiResponse<AIFeedbackDto>.ErrorResponse("AI chưa tạo được nhận xét. Vui lòng thử lại sau.");
                }

                // An all-empty payload = failure. Do NOT persist it, or the result page would show a
                // blank "AI analysis" card and its poll would never settle.
                if (string.IsNullOrWhiteSpace(fullSolution)
                    && string.IsNullOrWhiteSpace(mistakeAnalysis)
                    && string.IsNullOrWhiteSpace(improvementAdvice))
                {
                    _logger.LogWarning(
                        "AI feedback returned empty content for attempt {AttemptId} question {QuestionId}",
                        dto.AttemptId, dto.QuestionId);
                    return ApiResponse<AIFeedbackDto>.ErrorResponse("AI chưa tạo được nhận xét. Vui lòng thử lại sau.");
                }
            }

            var feedback = new AIFeedback
            {
                AttemptId = dto.AttemptId,
                QuestionId = dto.QuestionId,
                FullSolution = fullSolution,
                MistakeAnalysis = mistakeAnalysis,
                ImprovementAdvice = improvementAdvice
            };

            var created = await _feedbackRepository.CreateAsync(feedback);

            // P6 — cost visibility (not gated: auto feedback is part of the result flow).
            if (attempt.StudentId is int sid)
                await _quota.RecordFeedbackAsync(sid);

            return ApiResponse<AIFeedbackDto>.SuccessResponse(
                MapToDto(created),
                "Đã tạo nhận xét"
            );
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int feedbackId)
        {
            var success = await _feedbackRepository.DeleteAsync(feedbackId);

            return success
                ? ApiResponse<bool>.SuccessResponse(true, "Feedback deleted")
                : ApiResponse<bool>.ErrorResponse("Không tìm thấy nhận xét");
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
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Không tìm thấy nhận xét");

            return ApiResponse<AIFeedbackDto>.SuccessResponse(MapToDto(feedback));
        }

        public async Task<ApiResponse<AIFeedbackDto>> UpdateAsync(int feedbackId, UpdateAIFeedbackDto dto)
        {
            var existing = await _feedbackRepository.GetByIdAsync(feedbackId);
            if (existing == null)
                return ApiResponse<AIFeedbackDto>.ErrorResponse("Không tìm thấy nhận xét");

            existing.FullSolution = dto.FullSolution;
            existing.MistakeAnalysis = dto.MistakeAnalysis;
            existing.ImprovementAdvice = dto.ImprovementAdvice;

            var updated = await _feedbackRepository.UpdateAsync(existing);

            return ApiResponse<AIFeedbackDto>.SuccessResponse(
                MapToDto(updated!),
                "Đã cập nhật nhận xét"
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
