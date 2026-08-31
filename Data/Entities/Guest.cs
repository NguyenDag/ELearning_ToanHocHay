using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // HỌC & LÀM BÀI KHI CHƯA ĐĂNG NHẬP (§5.14)
    // GuestSession (cookie) + GuestIpUsage (chống xoá cookie).
    // Tiến độ CHỈ lưu cho tài khoản đã đăng ký — khách chỉ lưu bài làm.
    // ============================================================

    [Table("GuestSession")]
    public class GuestSession
    {
        [Key]
        public Guid GuestSessionId { get; set; } = Guid.NewGuid();

        public int? GradeLevelId { get; set; }

        public int LessonViewCount { get; set; }
        public int AttemptCount { get; set; }

        public int? ConvertedToStudentId { get; set; }
        public DateTime? ConvertedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public GradeLevel? GradeLevel { get; set; }
        public Student? ConvertedToStudent { get; set; }
    }

    [Table("GuestIpUsage")]
    public class GuestIpUsage
    {
        [Required, MaxLength(64)]
        public string IpHash { get; set; }            // khoá kép với Date
        public DateOnly Date { get; set; }

        public int LessonViewCount { get; set; }
        public int AttemptCount { get; set; }
    }
}
