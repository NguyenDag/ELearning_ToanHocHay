using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // MUA KHOÁ HỌC + THANH TOÁN (§5.10) + PHÂN QUYỀN THEO GÓI (§5.7)
    // ============================================================

    public enum OrderStatus
    {
        Pending,
        Paid,
        Cancelled,
        Refunded
    }

    public enum OrderItemType
    {
        Course,
        Package,
        Bundle
    }

    [Table("Order")]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int BuyerUserId { get; set; }          // học sinh hoặc phụ huynh

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubtotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }      // = Subtotal - Discount

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        // Navigation
        public User? Buyer { get; set; }
        public ICollection<OrderItem> Items { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<PromotionRedemption> PromotionRedemptions { get; set; }
    }

    [Table("OrderItem")]
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }

        public OrderItemType ItemType { get; set; }

        public int? CourseId { get; set; }
        public int? PackageId { get; set; }
        public int? CourseBundleId { get; set; }

        public int BeneficiaryStudentId { get; set; } // con nào được hưởng

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }   // promo phân bổ xuống dòng

        public int Quantity { get; set; } = 1;

        // Navigation
        public Order? Order { get; set; }
        public Course? Course { get; set; }
        public Package? Package { get; set; }
        public CourseBundle? Bundle { get; set; }
        public Student? BeneficiaryStudent { get; set; }
    }

    public enum EntitlementScope
    {
        AllContent,
        Subject,
        Grade,
        SubjectGrade,
        Course
    }

    [Table("PackageEntitlement")]
    public class PackageEntitlement
    {
        [Key]
        public int PackageEntitlementId { get; set; }

        public int PackageId { get; set; }

        public EntitlementScope ScopeType { get; set; }

        public int? SubjectId { get; set; }
        public int? GradeLevelId { get; set; }
        public int? CourseId { get; set; }

        // Navigation
        public Package? Package { get; set; }
        public Subject? Subject { get; set; }
        public GradeLevel? GradeLevel { get; set; }
        public Course? Course { get; set; }
    }

    // Gói gia đình (§11) — thành viên của một subscription.
    [Table("SubscriptionMember")]
    public class SubscriptionMember
    {
        [Key]
        public int SubscriptionMemberId { get; set; }

        public int SubscriptionId { get; set; }
        public int StudentId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RemovedAt { get; set; }

        // Navigation
        public Subscription? Subscription { get; set; }
        public Student? Student { get; set; }
    }
}
