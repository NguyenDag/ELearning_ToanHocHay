using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Models.DTOs.Subscription;
using ELearning_ToanHocHay_Control.Repositories.Implementations;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SubscriptionPaymentService : ISubscriptionPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ISubscriptionRepository _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPackageRepository _packageRepository;
        private readonly AppDbContext _context;
        private readonly SePayOptions _sePayOptions;

        public SubscriptionPaymentService(
            IPaymentRepository paymentRepo,
            ISubscriptionRepository subscriptionRepo,
            IUnitOfWork unitOfWork,
            IPackageRepository packageRepository,
            AppDbContext context,
            IOptions<SePayOptions> sePayOptions)
        {
            _paymentRepo = paymentRepo;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
            _packageRepository = packageRepository;
            _context = context;
            _sePayOptions = sePayOptions.Value;
        }

        public async Task<ApiResponse<CreatePendingResultDto>> CreatePendingAsync(CreateSubscriptionDto dto, int paidByUserId)
        {
            var qrTtl = TimeSpan.FromMinutes(Math.Max(1, _sePayOptions.QrTimeoutMinutes));

            var package = await _packageRepository.GetByIdAsync(dto.PackageId);
            if (package == null)
            {
                return ApiResponse<CreatePendingResultDto>.ErrorResponse("Không tìm thấy gói cước");
            }

            // Price is decided by the server, never by the client (A2-02).
            var amount = package.Price;

            // Tải lại trang thanh toán KHÔNG tạo đơn mới: nếu học sinh đã có một đơn Pending cho
            // đúng gói này và QR vẫn còn hiệu lực, dùng lại đơn đó (giữ nguyên đồng hồ đếm ngược).
            var reuseFrom = DateTime.UtcNow - qrTtl;
            var existingPending = await _context.Subscriptions
                .Where(s => s.StudentId == dto.StudentId
                            && s.PackageId == dto.PackageId
                            && s.Status == SubscriptionStatus.Pending
                            && s.CreatedAt > reuseFrom)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingPending != null)
            {
                return ApiResponse<CreatePendingResultDto>.SuccessResponse(
                    new CreatePendingResultDto
                    {
                        SubscriptionId = existingPending.SubscriptionId,
                        Amount = existingPending.AmountPaid,
                        CreatedAt = existingPending.CreatedAt,
                        ExpiresAt = existingPending.CreatedAt + qrTtl
                    },
                    "Pending subscription reused");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {

                // 1. Create the payment first
                var payment = new Payment
                {
                    PaidByUserId = paidByUserId,
                    StudentId = dto.StudentId,
                    Amount = amount,
                    PaymentMethod = PaymentMethod.BankTransfer,
                    Status = PaymentStatus.Pending,
                    Notes = "SePay payment"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // 2. Create the pending subscription
                var subscription = new Subscription
                {
                    StudentId = dto.StudentId,
                    PackageId = dto.PackageId,
                    Payment = payment,
                    AmountPaid = amount,
                    Status = SubscriptionStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                return ApiResponse<CreatePendingResultDto>.SuccessResponse(
                    new CreatePendingResultDto
                    {
                        SubscriptionId = subscription.SubscriptionId,
                        Amount = amount,
                        CreatedAt = subscription.CreatedAt,
                        ExpiresAt = subscription.CreatedAt + qrTtl
                    },
                    "Pending subscription created");
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
