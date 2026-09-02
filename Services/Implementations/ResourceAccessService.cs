using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ResourceAccessService : IResourceAccessService
    {
        private readonly IStudentRepository _studentRepo;
        private readonly IParentRepository _parentRepo;
        private readonly IParentLinkRepository _parentLinkRepo;
        private readonly IExerciseAttemptRepository _attemptRepo;
        private readonly ISubscriptionRepository _subscriptionRepo;
        private readonly IPaymentRepository _paymentRepo;

        public ResourceAccessService(
            IStudentRepository studentRepo,
            IParentRepository parentRepo,
            IParentLinkRepository parentLinkRepo,
            IExerciseAttemptRepository attemptRepo,
            ISubscriptionRepository subscriptionRepo,
            IPaymentRepository paymentRepo)
        {
            _studentRepo = studentRepo;
            _parentRepo = parentRepo;
            _parentLinkRepo = parentLinkRepo;
            _attemptRepo = attemptRepo;
            _subscriptionRepo = subscriptionRepo;
            _paymentRepo = paymentRepo;
        }

        public async Task<bool> CanAccessStudentAsync(int studentId, int userId, UserType? userType)
        {
            if (userType == UserType.SystemAdmin) return true;

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null) return false;

            // The student themselves.
            if (student.UserId == userId) return true;

            // A parent with an active link.
            var parent = await _parentRepo.GetByUserIdAsync(userId);
            if (parent != null)
                return await _parentLinkRepo.ExistsActiveAsync(studentId, parent.ParentId);

            return false;
        }

        public Task<bool> CanAccessStudentAsync(ClaimsPrincipal user, int studentId)
        {
            var userId = user.GetUserId();
            if (userId == null) return Task.FromResult(false);
            return CanAccessStudentAsync(studentId, userId.Value, user.GetUserType());
        }

        public async Task<bool> CanModifyAttemptAsync(ClaimsPrincipal user, int attemptId)
        {
            var studentId = user.GetStudentId();
            if (studentId == null) return false;

            var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId);
            return attempt != null && attempt.StudentId == studentId.Value;
        }

        public async Task<bool> CanViewAttemptAsync(ClaimsPrincipal user, int attemptId)
        {
            var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId);
            if (attempt?.StudentId == null) return false;
            return await CanAccessStudentAsync(user, attempt.StudentId.Value);
        }

        public async Task<bool> CanAccessSubscriptionAsync(ClaimsPrincipal user, int subscriptionId)
        {
            if (user.HasUserType(UserType.FinanceManager, UserType.SystemAdmin)) return true;

            var sub = await _subscriptionRepo.GetByIdAsync(subscriptionId);
            if (sub?.StudentId == null) return false;
            return await CanAccessStudentAsync(user, sub.StudentId.Value);
        }

        public async Task<bool> CanAccessPaymentAsync(ClaimsPrincipal user, int paymentId)
        {
            if (user.HasUserType(UserType.FinanceManager, UserType.SystemAdmin)) return true;

            var payment = await _paymentRepo.GetByIdAsync(paymentId);
            if (payment == null) return false;

            if (payment.PaidByUserId == user.GetUserId()) return true;
            if (payment.StudentId != null)
                return await CanAccessStudentAsync(user, payment.StudentId.Value);

            return false;
        }
    }
}
