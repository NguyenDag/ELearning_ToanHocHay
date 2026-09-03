using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Course;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ICourseService
    {
        Task<ApiResponse<List<CourseDto>>> GetCoursesAsync(int? subjectId, int? gradeLevelId, bool publishedOnly);
        Task<ApiResponse<CourseDto>> GetCourseAsync(int courseId);
        Task<ApiResponse<CourseDto>> GetCourseBySlugAsync(string slug);
        Task<ApiResponse<CourseDto>> CreateCourseAsync(CourseRequestDto dto, int userId);
        Task<ApiResponse<CourseDto>> UpdateCourseAsync(int courseId, CourseRequestDto dto);
        Task<ApiResponse<CourseDto>> SetCourseArchivedAsync(int courseId, bool archived);

        Task<ApiResponse<List<CourseVersionDto>>> GetVersionsAsync(int courseId);
        Task<ApiResponse<CourseVersionDto>> CreateVersionAsync(int courseId, CreateCourseVersionDto dto, int userId);
        Task<ApiResponse<CourseVersionDto>> SubmitVersionAsync(int versionId, int userId);
        Task<ApiResponse<CourseVersionDto>> ReviewVersionAsync(int versionId, ReviewCourseVersionDto dto, int reviewerId);
        Task<ApiResponse<CourseVersionDto>> PublishVersionAsync(int versionId, int userId);
        Task<ApiResponse<CourseVersionDto>> ArchiveVersionAsync(int versionId);

        Task<ApiResponse<List<ContentReviewDto>>> GetVersionReviewsAsync(int versionId);
        Task<ApiResponse<bool>> ResolveReviewCommentAsync(int commentId, int userId);
    }
}
