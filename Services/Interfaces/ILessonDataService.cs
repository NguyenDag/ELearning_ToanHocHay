using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ILessonDataService
    {
        Task<ApiResponse<LessonDetailResponseDto>> CreateLessonDataAsync(CreateLessonDataDto dto);
        Task<ApiResponse<LessonDetailResponseDto>> CreateOrAddLessonDataAsync(CreateOrAddLessonDataDto dto);
    }
}
