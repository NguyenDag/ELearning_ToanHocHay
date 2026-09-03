using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Question
{
    public class QuestionBankDto
    {
        public int BankId { get; set; }
        public string BankName { get; set; } = "";
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int GradeLevelId { get; set; }
        public string? GradeLevelName { get; set; }
        public int? CourseId { get; set; }
        public int? PrimaryNodeId { get; set; }
        public bool IsActive { get; set; }
        public int QuestionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class QuestionBankRequestDto
    {
        [Required, MaxLength(255)] public string BankName { get; set; } = "";
        public string? Description { get; set; }
        [Required] public int SubjectId { get; set; }
        [Required] public int GradeLevelId { get; set; }
        public int? CourseId { get; set; }
        public int? PrimaryNodeId { get; set; }
        public bool IsActive { get; set; } = false;
    }

    /// <summary>Full question incl. status + answer key — for authors/reviewers.</summary>
    public class QuestionAdminDto
    {
        public int QuestionId { get; set; }
        public int BankId { get; set; }
        public int SubjectId { get; set; }
        public string QuestionText { get; set; } = "";
        public string? QuestionImageUrl { get; set; }
        public QuestionType QuestionType { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public QuestionStatus Status { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public int? ReviewedBy { get; set; }
        public string? RejectReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    public class UpdateQuestionDto
    {
        [Required] public string QuestionText { get; set; } = "";
        public string? QuestionImageUrl { get; set; }
        [Required] public QuestionType QuestionType { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public List<CreateQuestionOptionDto> Options { get; set; } = new();
    }

    public class ReviewQuestionDto
    {
        /// <summary>true = approve, false = reject.</summary>
        [Required] public bool Approve { get; set; }
        public string? RejectReason { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
