// ============================================================
// FILE: ELearning_ToanHocHay_Control/Models/DTOs/Subscription/SubscriptionInfoDto.cs
// ============================================================
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Subscription
{
    /// <summary>
    /// Thông tin gói hiện tại của học sinh, embed vào CoreDashboardDto.
    /// PackageTier: Free / Standard / Premium / Yearly (số 0..3).
    /// </summary>
    public class SubscriptionInfoDto
    {
        public PackageTier PackageTier { get; set; } = PackageTier.Free;
        public string PackageName { get; set; } = "Free";
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = false;
        public int DaysRemaining { get; set; } = 0;

        // Feature flags lấy thẳng từ Package entity
        public bool UnlimitedAiHint { get; set; } = false;
        public int? AiHintLimitDaily { get; set; } = 0;
        public bool PersonalizedPath { get; set; } = false;
        public bool MistakeRetry { get; set; } = false;
        public bool SmartReminder { get; set; } = false;
        public bool PrioritySupport { get; set; } = false;
    }
}