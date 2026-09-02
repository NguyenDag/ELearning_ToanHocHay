using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>A3/P2 — question bank CRUD + question CRUD and the review workflow.</summary>
    public interface IQuestionBankService
    {
        // banks
        Task<ApiResponse<List<QuestionBankDto>>> GetBanksAsync(int? subjectId, int? gradeLevelId, bool includeInactive);
        Task<ApiResponse<QuestionBankDto>> GetBankAsync(int bankId);
        Task<ApiResponse<QuestionBankDto>> CreateBankAsync(QuestionBankRequestDto dto, int userId);
        Task<ApiResponse<QuestionBankDto>> UpdateBankAsync(int bankId, QuestionBankRequestDto dto);
        Task<ApiResponse<bool>> DeleteBankAsync(int bankId);

        // questions
        Task<ApiResponse<PagedResult<QuestionAdminDto>>> GetQuestionsAsync(
            int bankId, QuestionStatus? status, string? search, int page, int pageSize);
        Task<ApiResponse<QuestionAdminDto>> GetQuestionAsync(int questionId);
        Task<ApiResponse<QuestionAdminDto>> UpdateQuestionAsync(int questionId, UpdateQuestionDto dto);
        Task<ApiResponse<bool>> DeleteQuestionAsync(int questionId);

        // review workflow
        Task<ApiResponse<QuestionAdminDto>> SubmitQuestionAsync(int questionId);
        Task<ApiResponse<QuestionAdminDto>> ReviewQuestionAsync(int questionId, ReviewQuestionDto dto, int reviewerId);
    }
}
