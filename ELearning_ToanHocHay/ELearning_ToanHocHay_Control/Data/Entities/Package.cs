using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
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

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public int DurationDays { get; set; }

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
    }
}
