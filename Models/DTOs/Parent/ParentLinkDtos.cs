using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Parent
{
    public class CreateParentInviteDto
    {
        [MaxLength(255)] public string? InviteeEmail { get; set; }
        public ParentRelationship Relationship { get; set; } = ParentRelationship.Guardian;
        public int ExpiresInDays { get; set; } = 7;
    }

    public class ParentInviteDto
    {
        public int ParentInviteId { get; set; }
        public string Token { get; set; } = "";
        public string? InviteeEmail { get; set; }
        public ParentInviteStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LinkParentDto
    {
        /// <summary>A parent's ConnectionCode or a ParentInvite token.</summary>
        [Required] public string Code { get; set; } = "";
        public ParentRelationship Relationship { get; set; } = ParentRelationship.Guardian;
    }

    public class ParentLinkDto
    {
        public int ParentLinkId { get; set; }
        public int ParentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public ParentRelationship Relationship { get; set; }
        public LinkStatus Status { get; set; }
        public bool IsPrimaryGuardian { get; set; }
        public DateTime LinkedAt { get; set; }
    }

    public class ChildOverviewDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public int GradeLevel { get; set; }
        public PackageTier PackageTier { get; set; }
        public int WeeklyStudyMinutes { get; set; }
        public int WeeklyExercisesCompleted { get; set; }
        public decimal WeeklyAverageScore { get; set; }
        public int CurrentStreak { get; set; }
        public bool StudiedToday { get; set; }
    }
}
