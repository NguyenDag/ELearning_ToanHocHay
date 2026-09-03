using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // HOÀN TIỀN BÁN TỰ ĐỘNG (docs/Luong-hoan-tien.md)
    // RefundRequest (vòng đời yêu cầu) · RefundBatch (lô chi hộ) · RefundEvent (timeline truy vết)
    // Không có payout API: Finance xuất CSV → chuyển khoản tay → xác nhận.
    // ============================================================

    public enum RefundReasonCode
    {
        DuplicatePayment,
        Overpayment,
        ServiceNotDelivered,
        CustomerRequest,
        BillingError,
        Goodwill,
        Other
    }

    public enum RefundRequestStatus
    {
        PendingReview,
        PendingSecondApproval,
        Approved,
        Batched,
        Disbursed,
        Completed,
        Rejected,
        Cancelled,
        Failed
    }

    public enum RefundBatchStatus
    {
        Draft,
        Exported,
        Disbursed,
        Completed,
        Cancelled
    }

    public enum RefundEventType
    {
        Created,
        Approved,
        SecondApproved,
        Rejected,
        Cancelled,
        AddedToBatch,
        RemovedFromBatch,
        BatchExported,
        MarkedDisbursed,
        Confirmed,
        MarkedFailed,
        RetryQueued
    }

    [Table("RefundRequest")]
    public class RefundRequest
    {
        [Key]
        public int RefundRequestId { get; set; }

        /// <summary>Client-facing id; also the reference put in the bank transfer content.</summary>
        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int PaymentId { get; set; }

        /// <summary>Who clicked "create" — a student/parent, or a Finance actor.</summary>
        public int RequestedByUserId { get; set; }

        /// <summary>True when a Finance user created it for someone else.</summary>
        public bool OnBehalf { get; set; }

        /// <summary>Who gets the money back — normally <see cref="Payment.PaidByUserId"/>.</summary>
        public int BeneficiaryUserId { get; set; }

        public RefundReasonCode ReasonCode { get; set; }

        [MaxLength(500)]
        public string? ReasonNote { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public RefundRequestStatus Status { get; set; } = RefundRequestStatus.PendingReview;

        // --- Beneficiary bank account (số TK mã hoá bằng Data Protection) ---
        [MaxLength(12)]
        public string BankBin { get; set; } = "";

        /// <summary>Data Protection ciphertext of the full account number (base64).</summary>
        [Column(TypeName = "text")]
        public string BankAccountNumberProtected { get; set; } = "";

        [MaxLength(4)]
        public string BankAccountNumberLast4 { get; set; } = "";

        [MaxLength(120)]
        public string BankAccountHolderName { get; set; } = "";

        // --- Approval (dual-control khi Amount >= refund.dualControlThresholdVnd) ---
        public int? FirstApprovedByUserId { get; set; }
        public DateTime? FirstApprovedAt { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public int? RejectedByUserId { get; set; }
        public DateTime? RejectedAt { get; set; }
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public int? RefundBatchId { get; set; }

        /// <summary>Bank's transfer reference, captured at confirm.</summary>
        [MaxLength(255)]
        public string? BankTransactionRef { get; set; }

        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }
        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Payment? Payment { get; set; }
        public RefundBatch? RefundBatch { get; set; }
        public ICollection<RefundEvent> Events { get; set; } = new List<RefundEvent>();
    }

    [Table("RefundBatch")]
    public class RefundBatch
    {
        [Key]
        public int RefundBatchId { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public RefundBatchStatus Status { get; set; } = RefundBatchStatus.Draft;

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ExportedByUserId { get; set; }
        public DateTime? ExportedAt { get; set; }

        public int? DisbursedByUserId { get; set; }
        public DateTime? DisbursedAt { get; set; }
        [MaxLength(500)]
        public string? DisbursementNote { get; set; }

        public int ItemCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime? CancelledAt { get; set; }

        // Navigation
        public ICollection<RefundRequest> Items { get; set; } = new List<RefundRequest>();
    }

    [Table("RefundEvent")]
    public class RefundEvent
    {
        [Key]
        public long RefundEventId { get; set; }

        public int? RefundRequestId { get; set; }
        public int? RefundBatchId { get; set; }

        public RefundEventType EventType { get; set; }

        [MaxLength(40)]
        public string? FromStatus { get; set; }
        [MaxLength(40)]
        public string? ToStatus { get; set; }

        /// <summary>Null = system / background job.</summary>
        public int? ActorUserId { get; set; }
        [MaxLength(40)]
        public string? ActorUserType { get; set; }
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        [MaxLength(64)]
        public string? CorrelationId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountSnapshot { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public RefundRequest? RefundRequest { get; set; }
        public RefundBatch? RefundBatch { get; set; }
    }
}
