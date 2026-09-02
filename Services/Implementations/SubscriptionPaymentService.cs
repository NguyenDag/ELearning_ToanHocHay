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

        public async Task<ApiResponse<CreatePendingResultDto>> CreatePendingAsync(CreateSubscriptionDto dto, int paidByUserId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var package = await _packageRepository.GetByIdAsync(dto.PackageId);
                if (package == null)
                {
                    return ApiResponse<CreatePendingResultDto>.ErrorResponse("Package not found");
                }

                // Price is decided by the server, never by the client (A2-02).
                var amount = package.Price;

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
                    Status = SubscriptionStatus.Pending
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                return ApiResponse<CreatePendingResultDto>.SuccessResponse(
                    new CreatePendingResultDto
                    {
                        SubscriptionId = subscription.SubscriptionId,
                        Amount = amount
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
