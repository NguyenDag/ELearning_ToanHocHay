using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // KHUYẾN MÃI (§5.15)
    // Promotion (luật) · PromotionScope (áp món nào) · PromotionRedemption (lượt dùng)
    // ============================================================

    public enum PromotionType
    {
        AutoApplied,
        Code
    }

    public enum DiscountKind
    {
        Percentage,
        FixedAmount,
        OverridePrice
    }

    public enum PromoScopeType
    {
        AllItems,
        Subject,
        GradeLevel,
        Course,
        Package
    }

    public enum RedemptionStatus
    {
        Reserved,
        Confirmed,
        Released,
        Voided
    }

    [Table("Promotion")]
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; }

        public PromotionType PromotionType { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }             // unique khi có

        public DiscountKind DiscountKind { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }    // 20(%), 50000(đ), hoặc giá ép

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }          // null = mở vô hạn

        public bool IsActive { get; set; } = true;
        public int Priority { get; set; }
        public bool Stackable { get; set; }

        public int? TotalUsageLimit { get; set; }     // cap toàn hệ
        public int PerUserLimit { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; }

        public bool FirstPurchaseOnly { get; set; }

        // Bộ đếm nguyên tử cho cap tổng
        public int ReservedCount { get; set; }
        public int ConfirmedCount { get; set; }

        public int CreatedBy { get; set; }            // Finance Manager
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<PromotionScope> Scopes { get; set; }
        public ICollection<PromotionRedemption> Redemptions { get; set; }
    }

    [Table("PromotionScope")]
    public class PromotionScope
    {
        [Key]
        public int PromotionScopeId { get; set; }

        public int PromotionId { get; set; }

        public PromoScopeType ScopeType { get; set; }

        public int? SubjectId { get; set; }
        public int? GradeLevelId { get; set; }
        public int? CourseId { get; set; }
        public int? PackageId { get; set; }

        // Navigation
        public Promotion? Promotion { get; set; }
    }

    [Table("PromotionRedemption")]
    public class PromotionRedemption
    {
        [Key]
        public int RedemptionId { get; set; }

        public int PromotionId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }

        public RedemptionStatus Status { get; set; } = RedemptionStatus.Reserved;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? VoidedAt { get; set; }

        // Navigation
        public Promotion? Promotion { get; set; }
        public Order? Order { get; set; }
        public User? User { get; set; }
    }
}
