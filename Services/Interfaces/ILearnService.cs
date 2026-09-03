using System.Security.Claims;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>A3/P2 — student/guest consumption of published content, gated by IContentAccessService.</summary>
    public interface ILearnService
    {
        Task<ApiResponse<CourseContentDto>> GetCourseContentAsync(ClaimsPrincipal user, int courseId);
        Task<ApiResponse<ContentNodeDetailDto>> GetNodeAsync(ClaimsPrincipal user, int nodeId);
    }

    /// <summary>A3/P2 — StudentCourse enrolment.</summary>
    public interface IEnrollmentService
    {
        Task<ApiResponse<List<EnrolmentDto>>> GetMyEnrolmentsAsync(int studentId);
        Task<ApiResponse<EnrolmentDto>> EnrollAsync(int studentId, int courseId);
    }
}
