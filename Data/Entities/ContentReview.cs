using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // DUYỆT NỘI DUNG (§5.9) — duyệt cả CourseVersion, không phải node lẻ
    // ============================================================

    public enum ReviewDecision
    {
        Approve,
        RequestChanges,
        Reject
    }

    public enum CommentStatus
    {
        Open,
        Resolved
    }

    [Table("ContentReview")]
    public class ContentReview
    {
        [Key]
        public int ReviewId { get; set; }

        public int CourseVersionId { get; set; }
        public int ReviewerId { get; set; }

        public ReviewDecision Decision { get; set; }

        public string? Summary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public CourseVersion? CourseVersion { get; set; }
        public User? Reviewer { get; set; }
        public ICollection<ReviewComment> Comments { get; set; }
    }

    [Table("ReviewComment")]
    public class ReviewComment
    {
        [Key]
        public int CommentId { get; set; }

        public int ReviewId { get; set; }

        public int? NodeId { get; set; }              // neo vào node / block cần sửa
        public int? BlockId { get; set; }

        [Required]
        public string Body { get; set; }

        public CommentStatus Status { get; set; } = CommentStatus.Open;

        public int? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ContentReview? Review { get; set; }
        public ContentNode? Node { get; set; }
        public ContentBlock? Block { get; set; }
    }
}
