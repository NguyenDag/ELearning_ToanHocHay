using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    /// <summary>
    /// The single package-tier standard (entity + DTOs). Numeric order matters:
    /// higher value = more access (used with >= comparisons).
    /// </summary>
    public enum PackageTier
    {
        Free = 0,
        Standard = 1,
        Premium = 2,
        Yearly = 3
    }

    [Table("Package")]
    public class Package
    {
        [Key]
        public int PackageId { get; set; }

        [Required]
        public int UserId { get; set; }   // người tạo gói

        [Required, MaxLength(100)]
        public string PackageName { get; set; }

        public string? Description { get; set; }

        public PackageTier Tier { get; set; } = PackageTier.Standard;  // thay so khớp chuỗi tên gói

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public int? MaxMembers { get; set; }          // gói gia đình (§11)

        public int? AiHintLimitDaily { get; set; }

        public bool UnlimitedAiHint { get; set; } = true;
        public bool PersonalizedPath { get; set; } = false;
        public bool MistakeRetry { get; set; } = false;
        public bool SmartReminder { get; set; } = false;
        public bool PrioritySupport { get; set; } = false;

        public string? FeaturesJson { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }

        // Navigation
        public User? User { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<PackageEntitlement> Entitlements { get; set; }
    }
}
