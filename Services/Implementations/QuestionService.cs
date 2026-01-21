using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        // Nếu bạn dùng AutoMapper thì inject vào, ở đây mình map tay cho chắc ăn

        public QuestionService(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public async Task<ApiResponse<QuestionDto>> CreateQuestionAsync(CreateQuestionDto dto)
        {
            try
            {
                // 1. Chuyển từ DTO (CreateQuestionDto) sang Entity (Question)
                var newQuestion = new Question
                {
                    QuestionText = dto.Content, // Map Content -> QuestionText
                    QuestionType = (QuestionType)dto.QuestionType, // Ép kiểu int sang enum (nếu cần)
                    Level = dto.Level,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1, // Tạm thời fix cứng Admin ID = 1

                    // Map danh sách đáp án
                    QuestionOptions = dto.Options.Select(o => new QuestionOption
                    {
                        OptionText = o.Content, // Map Content -> OptionText
                        IsCorrect = o.IsCorrect
                    }).ToList()
                };

                // 2. Gọi Repository lưu vào DB
                var savedQuestion = await _questionRepository.AddQuestionAsync(newQuestion);

                // 3. Chuyển ngược lại từ Entity sang DTO (QuestionDto) để trả về
                var resultDto = new QuestionDto
                {
                    QuestionId = savedQuestion.QuestionId,
                    Content = savedQuestion.QuestionText,
                    Options = savedQuestion.QuestionOptions.Select(o => new OptionDto
                    {
                        OptionId = o.OptionId,
                        Content = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                };

                return ApiResponse<QuestionDto>.SuccessResponse(resultDto, "Tạo câu hỏi thành công!");
            }
            catch (Exception ex)
            {
                return ApiResponse<QuestionDto>.ErrorResponse("Lỗi khi tạo câu hỏi", new List<string> { ex.Message });
            }
        }
    }
}