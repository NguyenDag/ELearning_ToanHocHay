using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    /// <summary>A3/P2 — StudentCourse enrolment + package-entitlement lookups.</summary>
    public interface IEnrollmentRepository
    {
        Task<StudentCourse?> GetActiveEnrolmentAsync(int studentId, int courseId);
        Task<List<StudentCourse>> GetEnrolmentsAsync(int studentId);
        Task<StudentCourse> AddEnrolmentAsync(StudentCourse enrolment);
        Task SaveAsync();

        /// <summary>Entitlements from the student's currently-active subscriptions.</summary>
        Task<List<PackageEntitlement>> GetActiveEntitlementsAsync(int studentId);
    }
}
