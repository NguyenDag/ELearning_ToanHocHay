using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled,
        Pending
    }

    [Table("Subscription")]
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        // Nullable — gói gia đình dùng SubscriptionMember thay vì 1 học sinh (§11).
        public int? StudentId { get; set; }
        public int PackageId { get; set; }
        public int? PaymentId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Student? Student { get; set; }
        public Package? Package { get; set; }
        public Payment? Payment { get; set; }
        public ICollection<SubscriptionMember> Members { get; set; }
    }
}
