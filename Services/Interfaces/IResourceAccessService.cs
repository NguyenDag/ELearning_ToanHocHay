using System.Security.Claims;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>
    /// Centralised resource ownership checks, shared by every controller.
    /// "Owner" = the student acting on their own data; a parent has read access only,
    /// via <c>ParentLink.Status = Active</c>; SystemAdmin has full access.
    /// </summary>
    public interface IResourceAccessService
    {
        Task<bool> CanAccessStudentAsync(int studentId, int userId, UserType? userType);
        Task<bool> CanAccessStudentAsync(ClaimsPrincipal user, int studentId);

        /// <summary>Only the student who owns the attempt (write / submit).</summary>
        Task<bool> CanModifyAttemptAsync(ClaimsPrincipal user, int attemptId);

        /// <summary>Owning student + linked parent + admin (view result / history).</summary>
        Task<bool> CanViewAttemptAsync(ClaimsPrincipal user, int attemptId);

        Task<bool> CanAccessSubscriptionAsync(ClaimsPrincipal user, int subscriptionId);
        Task<bool> CanAccessPaymentAsync(ClaimsPrincipal user, int paymentId);
    }
}
