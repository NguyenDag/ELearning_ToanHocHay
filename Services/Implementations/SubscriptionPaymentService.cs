using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Subscription;
using ELearning_ToanHocHay_Control.Repositories.Implementations;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SubscriptionPaymentService : ISubscriptionPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ISubscriptionRepository _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPackageRepository _packageRepository;
        private readonly AppDbContext _context;

        public SubscriptionPaymentService(
            IPaymentRepository paymentRepo,
            ISubscriptionRepository subscriptionRepo,
            IUnitOfWork unitOfWork,
            IPackageRepository packageRepository,
            AppDbContext context)
        {
            _paymentRepo = paymentRepo;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
            _packageRepository = packageRepository;
            _context = context;
        }

        public async Task<ApiResponse<int>> CreatePendingAsync(CreateSubscriptionDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var package = await _packageRepository.GetByIdAsync(dto.PackageId);
                if (package == null)
                {
                    return ApiResponse<int>.ErrorResponse("Gói học không tồn tại");
                }

                // 1. Tạo payment trước
                var payment = new Payment
                {
                    StudentId = dto.StudentId,
                    Amount = dto.AmountPaid,
                    PaymentMethod = PaymentMethod.BankTransfer,
                    Status = PaymentStatus.Pending,
                    Notes = "Thanh toán SePay"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // 2. Tạo subscription pending
                var subscription = new Subscription
                {
                    StudentId = dto.StudentId,
                    PackageId = dto.PackageId,
                    Payment = payment,
                    AmountPaid = dto.AmountPaid,
                    Status = SubscriptionStatus.Pending
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                return ApiResponse<int>
                    .SuccessResponse(subscription.SubscriptionId, "Tạo subscription chờ thanh toán");
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
