using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum NotificationType
    {
        Info,
        Warning,
        Success,
        Error,
        Reminder
    }

    public enum NotifyAudience
    {
        Student,
        Parent,
        Both,
        Staff
    }

    [Table("Notification")]
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; }               // người nhận cụ thể

        public int? StudentId { get; set; }           // thông báo "về" học sinh nào

        public NotifyAudience Audience { get; set; } = NotifyAudience.Student;

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public NotificationType NotificationType { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation
        public User? User { get; set; }
        public Student? Student { get; set; }
    }
}
