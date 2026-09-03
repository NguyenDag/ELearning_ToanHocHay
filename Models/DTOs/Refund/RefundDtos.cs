using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Refund
{
    public class CreateRefundRequestDto
    {
        [Required]
        public int PaymentId { get; set; }

        /// <summary>Số tiền hoàn; bỏ trống = hoàn toàn bộ phần còn lại.</summary>
        [Range(0.01, 999_999_999)]
        public decimal? Amount { get; set; }

        [Required]
        public RefundReasonCode ReasonCode { get; set; }

        [MaxLength(500)]
        public string? ReasonNote { get; set; }

        [Required, RegularExpression(@"^\d{4,12}$", ErrorMessage = "BankBin phải là 4–12 chữ số (mã napas).")]
        public string BankBin { get; set; } = "";

        [Required, RegularExpression(@"^\d{6,20}$", ErrorMessage = "Số tài khoản phải là 6–20 chữ số.")]
        public string BankAccountNumber { get; set; } = "";

        [Required, MaxLength(120)]
        public string BankAccountHolderName { get; set; } = "";
    }

    public class RefundRequestDto
    {
        public int RefundRequestId { get; set; }
        public Guid PublicId { get; set; }
        public int PaymentId { get; set; }
        public int BeneficiaryUserId { get; set; }
        public bool OnBehalf { get; set; }
        public RefundReasonCode ReasonCode { get; set; }
        public string? ReasonNote { get; set; }
        public decimal Amount { get; set; }
        public RefundRequestStatus Status { get; set; }
        public string BankBin { get; set; } = "";
        public string BankAccountNumberLast4 { get; set; } = "";
        public string BankAccountHolderName { get; set; } = "";
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? RefundBatchId { get; set; }
        public string? BankTransactionRef { get; set; }
        public string? RejectionReason { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefundEventDto
    {
        public RefundEventType EventType { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public int? ActorUserId { get; set; }
        public string? ActorUserType { get; set; }
        public decimal? AmountSnapshot { get; set; }
        public string? Note { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefundRequestDetailDto : RefundRequestDto
    {
        public List<RefundEventDto> Events { get; set; } = new();
    }

    public class ApproveRefundDto
    {
        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class RejectRefundDto
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = "";
    }

    public class CancelRefundDto
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class ConfirmRefundDto
    {
        [Required, MaxLength(255)]
        public string BankTransactionRef { get; set; } = "";

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class MarkRefundFailedDto
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = "";
    }

    public class CreateRefundBatchDto
    {
        /// <summary>Bỏ trống = gộp tất cả yêu cầu đang ở trạng thái Approved.</summary>
        public List<int>? RefundRequestIds { get; set; }
    }

    public class MarkBatchDisbursedDto
    {
        [MaxLength(500)]
        public string? Note { get; set; }
        public DateTime? DisbursedAt { get; set; }
    }

    public class RefundBatchDto
    {
        public int RefundBatchId { get; set; }
        public Guid PublicId { get; set; }
        public RefundBatchStatus Status { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExportedAt { get; set; }
        public DateTime? DisbursedAt { get; set; }
        public string? DisbursementNote { get; set; }
    }

    public class RefundBatchDetailDto : RefundBatchDto
    {
        public List<RefundRequestDto> Items { get; set; } = new();
    }

    public class RefundDailyUsageDto
    {
        public decimal CapVnd { get; set; }
        public decimal UsedVnd { get; set; }
        public decimal RemainingVnd { get; set; }
        public DateTime WindowStartUtc { get; set; }
        public DateTime ResetAtUtc { get; set; }
    }
}
