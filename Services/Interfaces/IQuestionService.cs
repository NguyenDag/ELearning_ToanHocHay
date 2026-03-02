using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IQuestionService
    {
        // Hàm tạo câu hỏi mới
        Task<ApiResponse<QuestionDto>> CreateQuestionAsync(CreateQuestionDto dto);

        // (Nếu có các hàm khác cũ thì giữ nguyên, chỉ thêm hàm trên vào)
    }
}