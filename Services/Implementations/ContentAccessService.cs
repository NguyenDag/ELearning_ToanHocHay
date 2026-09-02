using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ContentAccessService : IContentAccessService
    {
        private readonly IEnrollmentRepository _enrolment;

        public ContentAccessService(IEnrollmentRepository enrolment)
        {
            _enrolment = enrolment;
        }

        public async Task<ContentAccessLevel> GetCourseAccessAsync(ClaimsPrincipal user, Course course)
        {
            var isEditor = user.HasUserType(
                UserType.ContentEditor, UserType.AcademicReviewer, UserType.SystemAdmin);

            if (course.Status != CourseStatus.Published)
                return isEditor ? ContentAccessLevel.Full : ContentAccessLevel.None;

            if (isEditor) return ContentAccessLevel.Full;

            var studentId = user.GetStudentId();
            if (studentId == null) return ContentAccessLevel.FreeOnly; // anonymous / parent

            return await HasCourseEntitlementAsync(studentId.Value, course)
                ? ContentAccessLevel.Full
                : ContentAccessLevel.FreeOnly;
        }

        public async Task<bool> HasCourseEntitlementAsync(int studentId, Course course)
        {
            if (await _enrolment.GetActiveEnrolmentAsync(studentId, course.CourseId) != null)
                return true;

            var entitlements = await _enrolment.GetActiveEntitlementsAsync(studentId);
            return entitlements.Any(e => Covers(e, course));
        }

        private static bool Covers(PackageEntitlement e, Course course) => e.ScopeType switch
        {
            EntitlementScope.AllContent => true,
            EntitlementScope.Subject => e.SubjectId == course.SubjectId,
            EntitlementScope.Grade => e.GradeLevelId == course.GradeLevelId,
            EntitlementScope.SubjectGrade => e.SubjectId == course.SubjectId && e.GradeLevelId == course.GradeLevelId,
            EntitlementScope.Course => e.CourseId == course.CourseId,
            _ => false
        };
    }
}
