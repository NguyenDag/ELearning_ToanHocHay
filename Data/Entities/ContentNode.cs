using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // TẦNG 3 — CÂY NỘI DUNG (ContentNode tự tham chiếu)
    // Chương / Chủ đề / Bài học đều là node; độ sâu do dữ liệu quyết định.
    // Node KHÔNG có workflow riêng — hiển thị theo CourseVersion.State.
    // ============================================================

    public enum NodeType
    {
        Chapter,
        Topic,
        SubTopic,
        Lesson
    }

    [Table("ContentNode")]
    public class ContentNode
    {
        [Key]
        public int NodeId { get; set; }

        public int CourseVersionId { get; set; }      // thuộc về 1 phiên bản khoá học

        public int? ParentNodeId { get; set; }        // self-FK; null = gốc

        public NodeType NodeType { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [MaxLength(255)]
        public string? Slug { get; set; }

        public int OrderIndex { get; set; }

        public int Depth { get; set; }

        [Required, MaxLength(400)]
        public string MaterializedPath { get; set; } = "/"; // "/12/48/193/"

        public bool IsFree { get; set; } = false;

        public bool IsHidden { get; set; } = false;   // ẩn mềm trong version đã publish

        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public CourseVersion? CourseVersion { get; set; }
        public ContentNode? Parent { get; set; }
        public ICollection<ContentNode> Children { get; set; }

        public LessonDetail? LessonDetail { get; set; }
        public ICollection<ContentBlock> Blocks { get; set; }
        public ICollection<LessonResource> Resources { get; set; }
        public ICollection<FlashcardDeck> FlashcardDecks { get; set; }
        public ICollection<NodeRevision> Revisions { get; set; }
        public ICollection<NodeSkill> NodeSkills { get; set; }
        public ICollection<QuestionNode> QuestionNodes { get; set; }
        public ICollection<Exercise> Exercises { get; set; }
        public ICollection<NodeProgress> NodeProgresses { get; set; }
    }

    // Luật lồng nhau — là DỮ LIỆU, chỉnh theo môn (không cần migration).
    [Table("NodeTypeRule")]
    public class NodeTypeRule
    {
        [Key]
        public int NodeTypeRuleId { get; set; }

        public int? SubjectId { get; set; }           // null = luật mặc định
        public NodeType? ParentType { get; set; }     // null = node gốc
        public NodeType ChildType { get; set; }

        // Navigation
        public Subject? Subject { get; set; }
    }

    // Mở rộng 1:1 cho node kiểu Lesson.
    [Table("LessonDetail")]
    public class LessonDetail
    {
        [Key]
        public int NodeId { get; set; }               // PK + FK

        public int? DurationMinutes { get; set; }

        // Navigation
        public ContentNode? Node { get; set; }
    }

    // Lịch sử chỉnh sửa từng node (diff & rollback mức node).
    [Table("NodeRevision")]
    public class NodeRevision
    {
        [Key]
        public long RevisionId { get; set; }

        public int NodeId { get; set; }
        public int RevisionNumber { get; set; }

        public string? Snapshot { get; set; }         // JSON

        public int EditedBy { get; set; }
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ContentNode? Node { get; set; }
    }
}
