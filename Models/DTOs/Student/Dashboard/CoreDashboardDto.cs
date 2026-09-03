using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Subscription;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard
{
    public class CoreDashboardDto
    {
        // Essential data only
        public StudentInfoDto StudentInfo { get; set; }
        public OverviewStatsDto Stats { get; set; }
        public List<RecentLessonDto> RecentLessons { get; set; }  // Max 5
        public List<ChapterProgressSummaryDto> ChapterProgress { get; set; }
        public PackageTier PackageTier { get; set; }
        public SubscriptionInfoDto SubscriptionInfo { get; set; } = new();

        // Links to lazy-load endpoints
        public DashboardLinksDto Links { get; set; }
    }
}
