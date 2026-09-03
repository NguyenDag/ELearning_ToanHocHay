using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuestionBankRepository _bankRepository;

        public QuestionService(IQuestionRepository questionRepository, IQuestionBankRepository bankRepository)
        {
            _questionRepository = questionRepository;
            _bankRepository = bankRepository;
        }

        public async Task<ApiResponse<QuestionDto>> CreateQuestionAsync(CreateQuestionDto dto, int createdBy)
        {
            try
            {
                var bank = await _bankRepository.GetQuestionBankByIdAsync(dto.BankId);
                if (bank == null)
                    return ApiResponse<QuestionDto>.ErrorResponse("Không tìm thấy ngân hàng câu hỏi");

                var question = BuildQuestion(dto, bank.SubjectId, createdBy);
                var result = await _questionRepository.CreateAsync(question);
                return ApiResponse<QuestionDto>.SuccessResponse(ToDto(result), "Tạo câu hỏi thành công!");
            }
            catch (Exception)
            {
                return ApiResponse<QuestionDto>.ErrorResponse("Lỗi khi tạo câu hỏi", new List<string>());
            }
        }

        public async Task<ApiResponse<List<QuestionDto>>> CreateQuestionsAsync(List<CreateQuestionDto> dtos, int createdBy)
        {
            try
            {
                if (dtos.Count == 0)
                    return ApiResponse<List<QuestionDto>>.ErrorResponse("Danh sách câu hỏi trống");

                var bankIds = dtos.Select(d => d.BankId).Distinct().ToList();
                var subjectByBank = new Dictionary<int, int>();
                foreach (var bankId in bankIds)
                {
                    var bank = await _bankRepository.GetQuestionBankByIdAsync(bankId);
                    if (bank == null)
                        return ApiResponse<List<QuestionDto>>.ErrorResponse($"Không tìm thấy ngân hàng câu hỏi {bankId}");
                    subjectByBank[bankId] = bank.SubjectId;
                }

                var questions = dtos
                    .Select(dto => BuildQuestion(dto, subjectByBank[dto.BankId], createdBy))
                    .ToList();

                var result = await _questionRepository.CreateMultipleAsync(questions);
                return ApiResponse<List<QuestionDto>>.SuccessResponse(
                    result.Select(ToDto).ToList(), "Tạo các câu hỏi thành công!");
            }
            catch (Exception)
            {
                return ApiResponse<List<QuestionDto>>.ErrorResponse("Lỗi khi tạo các câu hỏi", new List<string>());
            }
        }

        private static Question BuildQuestion(CreateQuestionDto dto, int subjectId, int createdBy) => new()
        {
            BankId = dto.BankId,
            SubjectId = subjectId,
            QuestionText = dto.QuestionText,
            QuestionImageUrl = dto.QuestionImageUrl,
            QuestionType = dto.QuestionType,
            DifficultyLevel = dto.DifficultyLevel,
            CorrectAnswer = dto.CorrectAnswer,
            Explanation = dto.Explanation,
            Status = QuestionStatus.Draft, // author submits it for review explicitly
            CreatedBy = createdBy, // A2-13 — from the caller's token
            CreatedAt = DateTime.UtcNow,
            QuestionOptions = dto.Options?.Select(o => new QuestionOption
            {
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect,
                OrderIndex = o.OrderIndex
            }).ToList()
        };

        private static QuestionDto ToDto(Question q) => new()
        {
            QuestionId = q.QuestionId,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType,
            DifficultyLevel = q.DifficultyLevel,
            Options = q.QuestionOptions?.Select(o => new QuestionOptionDto
            {
                OptionId = o.OptionId,
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect
            }).ToList()
        };
    }
}
