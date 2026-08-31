using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // LIÊN KẾT PHỤ HUYNH (§5.11) — ParentLink thay StudentParent phẳng
    // ============================================================

    public enum ParentRelationship
    {
        Father,
        Mother,
        Guardian,
        Other
    }

    public enum LinkStatus
    {
        Pending,
        Active,
        Revoked
    }

    [Table("ParentLink")]
    public class ParentLink
    {
        [Key]
        public int ParentLinkId { get; set; }

        public int ParentId { get; set; }
        public int StudentId { get; set; }

        public ParentRelationship Relationship { get; set; }

        public LinkStatus Status { get; set; } = LinkStatus.Pending;

        public bool IsPrimaryGuardian { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }

        // Navigation
        public Parent? Parent { get; set; }
        public Student? Student { get; set; }
    }

    public enum ParentInviteStatus
    {
        Pending,
        Accepted,
        Expired,
        Cancelled
    }

    [Table("ParentInvite")]
    public class ParentInvite
    {
        [Key]
        public int ParentInviteId { get; set; }

        public int ParentId { get; set; }

        [MaxLength(255)]
        public string? InviteeEmail { get; set; }

        [Required, MaxLength(64)]
        public string Token { get; set; }

        public ParentInviteStatus Status { get; set; } = ParentInviteStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public int? AcceptedByStudentId { get; set; }

        // Navigation
        public Parent? Parent { get; set; }
        public Student? AcceptedByStudent { get; set; }
    }
}
