using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ILessonSevice
    {
        Task<ApiResponse<IEnumerable<LessonDto>>> GetByTopicAsync(int topicId);
        Task<ApiResponse<LessonDto>> GetByIdAsync(int lessonId);
        Task<ApiResponse<LessonDto>> CreateAsync(CreateLessonDto dto, int creatorId);
        Task<ApiResponse<LessonDto>> UpdateAsync(int lessonId, UpdateLessonDto dto);
        Task<ApiResponse<bool>> SubmitForReviewAsync(int lessonId);
        Task<ApiResponse<bool>> ReviewAsync(int lessonId, ReviewLessonDto dto, int reviewerId);
        Task<ApiResponse<bool>> PublishAsync(int lessonId);
    }
}
