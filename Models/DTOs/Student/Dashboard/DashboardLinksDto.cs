namespace ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard
{
    public class DashboardLinksDto
    {
        public string ExerciseHistory { get; set; }
        public string? Charts { get; set; }        // null nếu Free
        public string? AIInsights { get; set; }    // null nếu < Premium
        public string? Notifications { get; set; } // null nếu Free
    }
}
