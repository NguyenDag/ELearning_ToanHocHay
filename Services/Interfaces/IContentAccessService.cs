using System.Security.Claims;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public enum ContentAccessLevel
    {
        /// <summary>Course is not visible at all (unpublished, or archived to a non-editor).</summary>
        None,

        /// <summary>Only nodes flagged <c>IsFree</c> are readable.</summary>
        FreeOnly,

        /// <summary>The whole published tree is readable.</summary>
        Full
    }

    /// <summary>
    /// A3/P2 — the three-tier content gate (§5.7): anonymous / registered / entitled.
    /// Entitlement = an active StudentCourse enrolment or an active subscription whose
    /// package covers the course.
    /// </summary>
    public interface IContentAccessService
    {
        Task<ContentAccessLevel> GetCourseAccessAsync(ClaimsPrincipal user, Course course);
        Task<bool> HasCourseEntitlementAsync(int studentId, Course course);
    }
}
