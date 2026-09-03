using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _repo;
        private readonly ICourseRepository _courseRepo;
        private readonly IContentAccessService _access;

        public EnrollmentService(
            IEnrollmentRepository repo, ICourseRepository courseRepo, IContentAccessService access)
        {
            _repo = repo;
            _courseRepo = courseRepo;
            _access = access;
        }

        public async Task<ApiResponse<List<EnrolmentDto>>> GetMyEnrolmentsAsync(int studentId)
        {
            var items = await _repo.GetEnrolmentsAsync(studentId);
            return ApiResponse<List<EnrolmentDto>>.SuccessResponse(items.Select(Map).ToList());
        }

        public async Task<ApiResponse<EnrolmentDto>> EnrollAsync(int studentId, int courseId)
        {
            var course = await _courseRepo.GetCourseAsync(courseId, withVersions: true);
            if (course == null) return ApiResponse<EnrolmentDto>.ErrorResponse("Course not found");
            if (course.Status != CourseStatus.Published)
                return ApiResponse<EnrolmentDto>.ErrorResponse("Course is not published");

            var published = course.Versions?.FirstOrDefault(v => v.State == VersionState.Published);
            if (published == null)
                return ApiResponse<EnrolmentDto>.ErrorResponse("Course has no published version");

            var existing = await _repo.GetActiveEnrolmentAsync(studentId, courseId);
            if (existing != null)
                return ApiResponse<EnrolmentDto>.SuccessResponse(Map(existing), "Already enrolled");

            var entitled = await _access.HasCourseEntitlementAsync(studentId, course);

            var enrolment = new StudentCourse
            {
                StudentId = studentId,
                CourseId = courseId,
                CourseVersionId = published.CourseVersionId,
                Source = entitled ? EnrollSource.Subscription : EnrollSource.Self,
                Status = StudentCourseStatus.Active,
                ProgressPercent = 0,
                EnrolledAt = DateTime.UtcNow,
                AccessExpiresAt = course.AccessDurationDays.HasValue
                    ? DateTime.UtcNow.AddDays(course.AccessDurationDays.Value)
                    : null
            };
            await _repo.AddEnrolmentAsync(enrolment);

            // reload with navs for the response
            var saved = (await _repo.GetEnrolmentsAsync(studentId))
                .First(sc => sc.StudentCourseId == enrolment.StudentCourseId);
            return ApiResponse<EnrolmentDto>.SuccessResponse(Map(saved), "Enrolled");
        }

        private static EnrolmentDto Map(StudentCourse sc) => new()
        {
            StudentCourseId = sc.StudentCourseId,
            CourseId = sc.CourseId,
            CourseTitle = sc.Course?.Title ?? "",
            SubjectName = sc.Course?.Subject?.Name,
            GradeLevelName = sc.Course?.GradeLevel?.Name,
            CourseVersionId = sc.CourseVersionId,
            Source = sc.Source,
            Status = sc.Status,
            ProgressPercent = sc.ProgressPercent,
            EnrolledAt = sc.EnrolledAt,
            AccessExpiresAt = sc.AccessExpiresAt
        };
    }
}
