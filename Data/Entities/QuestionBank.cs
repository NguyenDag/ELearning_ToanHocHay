using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // §5.5 — QuestionBank theo Subject + Grade; gắn Course tuỳ chọn.
    [Table("QuestionBank")]
    public class QuestionBank
    {
        [Key]
        public int BankId { get; set; }

        [Required, MaxLength(255)]
        public string BankName { get; set; }

        public string? Description { get; set; }

        public int SubjectId { get; set; }            // bắt buộc
        public int GradeLevelId { get; set; }         // bắt buộc

        public int? CourseId { get; set; }            // tuỳ chọn — ngân hàng riêng cho khoá
        public int? PrimaryNodeId { get; set; }       // thay ChapterId?/TopicId?

        public int? CreatedBy { get; set; }

        public bool IsActive { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Subject? Subject { get; set; }
        public GradeLevel? GradeLevel { get; set; }
        public Course? Course { get; set; }
        public ContentNode? PrimaryNode { get; set; }
        public User? Creator { get; set; }
        public ICollection<Question> Questions { get; set; }
    }
}
