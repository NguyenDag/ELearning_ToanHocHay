using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Catalog;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ICatalogService
    {
        // Subject
        Task<ApiResponse<List<SubjectDto>>> GetSubjectsAsync(bool includeInactive);
        Task<ApiResponse<SubjectDto>> GetSubjectAsync(int id);
        Task<ApiResponse<SubjectDto>> CreateSubjectAsync(SubjectRequestDto dto);
        Task<ApiResponse<SubjectDto>> UpdateSubjectAsync(int id, SubjectRequestDto dto);

        // GradeLevel
        Task<ApiResponse<List<GradeLevelDto>>> GetGradeLevelsAsync(bool includeInactive);
        Task<ApiResponse<GradeLevelDto>> GetGradeLevelAsync(int id);
        Task<ApiResponse<GradeLevelDto>> CreateGradeLevelAsync(GradeLevelRequestDto dto);
        Task<ApiResponse<GradeLevelDto>> UpdateGradeLevelAsync(int id, GradeLevelRequestDto dto);

        // CurriculumFramework
        Task<ApiResponse<List<FrameworkDto>>> GetFrameworksAsync(bool includeInactive);
        Task<ApiResponse<FrameworkDto>> GetFrameworkAsync(int id);
        Task<ApiResponse<FrameworkDto>> CreateFrameworkAsync(FrameworkRequestDto dto);
        Task<ApiResponse<FrameworkDto>> UpdateFrameworkAsync(int id, FrameworkRequestDto dto);
    }
}
