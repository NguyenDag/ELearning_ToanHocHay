using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Package
{
    public class PackageDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; }
        public string? Description { get; set; }

        public PackageTier Tier { get; set; }

        public decimal Price { get; set; }
        public int DurationDays { get; set; }

        public int? MaxMembers { get; set; }
        public int? AiHintLimitDaily { get; set; }

        public bool UnlimitedAiHint { get; set; }
        public bool PersonalizedPath { get; set; }
        public bool MistakeRetry { get; set; }
        public bool SmartReminder { get; set; }
        public bool PrioritySupport { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdated { get; set; }

        /// <summary>Số thuê bao đang ở trạng thái Active gắn với gói này (quản trị tài chính).</summary>
        public int ActiveSubscriberCount { get; set; }
    }
}
