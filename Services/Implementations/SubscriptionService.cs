using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Subscription;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repository;

        public SubscriptionService(ISubscriptionRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<IEnumerable<SubscriptionDto>>> GetAllAsync()
        {
            var subs = await _repository.GetAllAsync();

            var data = subs.Select(x => new SubscriptionDto
            {
                SubscriptionId = x.SubscriptionId,
                StudentId = x.StudentId,
                PaymentId = x.PaymentId,
                PackageId = x.PackageId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                AmountPaid = x.AmountPaid,
                CreatedAt = x.CreatedAt,
            });

            return ApiResponse<IEnumerable<SubscriptionDto>>
                .SuccessResponse(data, "Lấy danh sách subscription thành công");
        }

        public async Task<ApiResponse<SubscriptionDto>> GetByIdAsync(int id)
        {
            var sub = await _repository.GetByIdAsync(id);
            if (sub == null)
                return ApiResponse<SubscriptionDto>
                    .ErrorResponse("Không tìm thấy subscription");

            var dto = new SubscriptionDto
            {
                SubscriptionId = sub.SubscriptionId,
                StudentId = sub.StudentId,
                PackageId = sub.PackageId,
                PaymentId= sub.PaymentId,
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                Status = sub.Status,
                AmountPaid = sub.AmountPaid,
                CreatedAt = sub.CreatedAt,
            };

            return ApiResponse<SubscriptionDto>
                .SuccessResponse(dto);
        }

        public async Task<ApiResponse<bool>> CancelAsync(int id)
        {
            var sub = await _repository.GetByIdAsync(id);
            if (sub == null)
                return ApiResponse<bool>
                    .ErrorResponse("Subscription không tồn tại");

            sub.Status = SubscriptionStatus.Cancelled;
            await _repository.UpdateAsync(sub);

            return ApiResponse<bool>
                .SuccessResponse(true, "Hủy subscription thành công");
        }

        public async Task<ApiResponse<bool>> CheckPremiumAsync(int studentId)
        {
            var active = await _repository.GetActiveByStudentAsync(studentId);

            return ApiResponse<bool>
                .SuccessResponse(active != null);
        }
    }
}
