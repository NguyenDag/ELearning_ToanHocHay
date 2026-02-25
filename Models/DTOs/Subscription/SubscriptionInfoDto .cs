// ============================================================
// FILE: ELearning_ToanHocHay_Control/Models/DTOs/Subscription/SubscriptionInfoDto.cs
// ============================================================
namespace ELearning_ToanHocHay_Control.Models.DTOs.Subscription
{
    /// <summary>
    /// Thông tin gói hiện tại của học sinh, embed vào CoreDashboardDto.
    /// PackageType: 0 = Free, 1 = Standard, 2 = Premium
    /// </summary>
    public class SubscriptionInfoDto
    {
        public int PackageType { get; set; } = 0;          // 0=Free, 1=Standard, 2=Premium
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