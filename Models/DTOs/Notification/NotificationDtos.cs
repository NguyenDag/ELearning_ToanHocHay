using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Notification
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int? StudentId { get; set; }
        public NotifyAudience Audience { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationType NotificationType { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationPreferenceDto
    {
        public string RuleKey { get; set; } = "";
        public bool Enabled { get; set; }
    }

    public class SetNotificationPreferenceDto
    {
        public string RuleKey { get; set; } = "";
        public bool Enabled { get; set; }
    }
}
