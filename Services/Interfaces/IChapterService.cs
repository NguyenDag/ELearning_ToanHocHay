using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Chapter;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IChapterService
    {
        Task<ApiResponse<IEnumerable<ChapterDto>>> GetByCurriculumAsync(int curriculumId);
        Task<ApiResponse<ChapterDto>> GetByIdAsync(int chapterId);
        Task<ApiResponse<ChapterDto>> CreateAsync(CreateChapterDto dto);
        Task<ApiResponse<ChapterDto>> UpdateAsync(int chapterId, UpdateChapterDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int chapterId);
    }
}
