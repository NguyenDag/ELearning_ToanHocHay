using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum LessonStatus
    {
        Draft,
        PendingReview,
        Approved,
        Published,
        Rejected
    }

    [Table("Lesson")]
    public class Lesson
    {
        [Key]
        public int LessonId { get; set; }

        public int TopicId { get; set; }

        [Required, MaxLength(255)]
        public string LessonName { get; set; }

        public string? Description { get; set; }

        public int? DurationMinutes { get; set; }

        public int OrderIndex { get; set; }

        public bool IsFree { get; set; } = false;
        public bool IsActive { get; set; } = false;

        public LessonStatus Status { get; set; }

        public int CreatedBy { get; set; }
        public int? ReviewedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? PublishedAt { get; set; }

        // Navigation
        public Topic? Topic { get; set; }
        public User? Creator { get; set; }
        public User? Reviewer { get; set; }
        public ICollection<LessonContent> LessonContents { get; set; }
    }
}
