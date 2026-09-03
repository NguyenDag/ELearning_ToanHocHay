using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<ApiResponse<QuestionDto>> CreateQuestionAsync(CreateQuestionDto dto, int createdBy);
        Task<ApiResponse<List<QuestionDto>>> CreateQuestionsAsync(List<CreateQuestionDto> dtos, int createdBy);
    }
}
