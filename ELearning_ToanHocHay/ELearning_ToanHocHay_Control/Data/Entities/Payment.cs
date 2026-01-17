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
        Refunded
    }

    [Table("Payment")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int StudentId { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        public string? Notes { get; set; }

        // Navigation
        public Student? Student { get; set; }
        public Subscription Subscription { get; set; }
    }
}
