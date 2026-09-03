using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Payment;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repository;
        private readonly IMapper _mapper;

        public PaymentService(IPaymentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<PaymentDto>>> GetAllAsync()
        {
            var payments = await _repository.GetAllAsync();

            var data = payments.Select(x => new PaymentDto
            {
                PaymentId = x.PaymentId,
                StudentId = x.StudentId ?? 0,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Status = x.Status,
                PaymentDate = x.PaymentDate,
                TransactionId = x.TransactionId
            });

            return ApiResponse<IEnumerable<PaymentDto>>
                .SuccessResponse(data, "Lấy danh sách payment thành công");
        }

        public async Task<ApiResponse<PaymentDto>> GetByIdAsync(int id)
        {
            var payment = await _repository.GetByIdAsync(id);
            if (payment == null)
                return ApiResponse<PaymentDto>.ErrorResponse("Payment không tồn tại");

            var dto = new PaymentDto
            {
                PaymentId = payment.PaymentId,
                StudentId = payment.StudentId ?? 0,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate,
                TransactionId = payment.TransactionId
            };

            return ApiResponse<PaymentDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<PagedResult<PaymentDto>>> GetMyPaymentsAsync(int userId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, total) = await _repository.GetForUserAsync(userId, page, pageSize);
            return ApiResponse<PagedResult<PaymentDto>>.SuccessResponse(new PagedResult<PaymentDto>
            {
                Items = items.Select(x => new PaymentDto
                {
                    PaymentId = x.PaymentId,
                    StudentId = x.StudentId ?? 0,
                    Amount = x.Amount,
                    PaymentMethod = x.PaymentMethod,
                    Status = x.Status,
                    PaymentDate = x.PaymentDate,
                    TransactionId = x.TransactionId
                }).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(int id, UpdatePaymentStatusDto dto)
        {
            var payment = await _repository.GetByIdAsync(id);
            if (payment == null)
                return ApiResponse<bool>.ErrorResponse("Payment không tồn tại",
                    new List<string> { $"No payment found with ID: {id}" }
                    );

            payment.Status = dto.Status;
            payment.TransactionId = dto.TransactionId;

            var updatedPayment = await _repository.UpdateAsync(payment);

            if (!updatedPayment)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error updating user",
                    new List<string> { "Failed to update user" }
                );
            }

            return ApiResponse<bool>
                .SuccessResponse(true, "Cập nhật trạng thái payment thành công");
        }
    }
}
