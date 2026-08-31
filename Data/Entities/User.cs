using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum UserType
    {
        Student,
        Parent,
        ContentEditor,
        AcademicReviewer,
        SupportStaff,
        FinanceManager,
        SystemAdmin
    }

    [Table("User")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(255)]
        public string Email { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required, MaxLength(255)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateOnly? Dob { get; set; }

        public string? AvatarUrl { get; set; }

        [Required]
        public UserType UserType { get; set; }        // mỗi người 1 vai; admin đổi được (ghi AuditLog)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsEmailConfirmed { get; set; } = false;
        public DateTime? EmailConfirmedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        public bool IsActive { get; set; } = true;

        // Khoá tài khoản (§5.13)
        public DateTime? LockedAt { get; set; }
        public string? LockedReason { get; set; }
        public int? LockedByUserId { get; set; }

        // Navigation
        public Student? Student { get; set; }
        public Parent? Parent { get; set; }

        public virtual ICollection<Package> Packages { get; set; }
        public virtual ICollection<Course> CreatedCourses { get; set; }

        public virtual ICollection<Question> CreatedQuestions { get; set; }
        public virtual ICollection<Question> ReviewedQuestions { get; set; }
        public virtual ICollection<Exercise> Exercises { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<SupportTicket> CreatedSupportTickets { get; set; }
        public virtual ICollection<SupportTicket> AssignedSupportTickets { get; set; }
        public virtual ICollection<SupportMessage> SupportMessages { get; set; }
        public virtual ICollection<AuditLog> AuditLogs { get; set; }
        public virtual ICollection<SystemConfig> SystemConfigs { get; set; }
    }
}
