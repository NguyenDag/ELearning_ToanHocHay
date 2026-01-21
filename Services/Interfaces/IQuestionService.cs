using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IQuestionService
    {
        // Hàm tạo câu hỏi mới
        Task<ApiResponse<QuestionDto>> CreateQuestionAsync(CreateQuestionDto dto);

        // (Nếu có các hàm khác cũ thì giữ nguyên, chỉ thêm hàm trên vào)
    }
}