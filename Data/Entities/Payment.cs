using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum PaymentMethod
    {
        CreditCard,
        BankTransfer,
        Momo,
        ZaloPay,
        VNPay
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded,
        PartiallyRefunded
    }

    [Table("Payment")]
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; }

        // Tách người trả khỏi người thụ hưởng (§5.10).
        // OrderId nullable: luồng thanh toán gói–thuê bao cũ chưa qua Order.
        public int? OrderId { get; set; }
        public int PaidByUserId { get; set; }         // người trả — có thể là phụ huynh
        public int? StudentId { get; set; }           // người thụ hưởng (tiện tra cứu)

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        public string? Notes { get; set; }

        public DateTime? RefundedAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }

        // Navigation
        public Order? Order { get; set; }
        public User? PaidByUser { get; set; }
        public Student? Student { get; set; }
        public Subscription? Subscription { get; set; }
    }
}
