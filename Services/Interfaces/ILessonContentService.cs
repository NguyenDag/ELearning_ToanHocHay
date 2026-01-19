using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ILessonContentService
    {
        Task<ApiResponse<IEnumerable<LessonContentDto>>> GetByLessonAsync(int lessonId);
        Task<ApiResponse<LessonContentDto>> GetByIdAsync(int contentId);
        Task<ApiResponse<LessonContentDto>> CreateAsync(int lessonId, CreateLessonContentDto dto);
        Task<ApiResponse<LessonContentDto>> UpdateAsync(int contentId, UpdateLessonContentDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int contentId);
    }
}
