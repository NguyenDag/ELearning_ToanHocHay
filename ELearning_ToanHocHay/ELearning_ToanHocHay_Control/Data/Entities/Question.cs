using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        FillBlank,
        Essay
    }

    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    public enum QuestionStatus
    {
        Draft,
        PendingReview,
        Approved,
        Rejected
    }

    [Table("Question")]
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        public int BankId { get; set; }

        public required string QuestionText { get; set; }

        [MaxLength(500)]
        public string? QuestionImageUrl { get; set; }

        public required QuestionType QuestionType { get; set; }

        public DifficultyLevel DifficultyLevel { get; set; }

        // Đáp án đúng (dùng cho TrueFalse, FillInBlank)
        public string? CorrectAnswer { get; set; }

        public string? Explanation { get; set; }

        public QuestionStatus Status { get; set; }
        public bool IsActive { get; set; } = true;

        public int CreatedBy { get; set; }
        public int? ReviewedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? RejectReason { get; set; }
        public DateTime? PublishedAt { get; set; }
        
        public int Version { get; set; } = 1;

        // Navigation
        public QuestionBank? QuestionBank { get; set; }
        public User? Creator { get; set; }
        public User? Reviewer { get; set; }
        public ICollection<QuestionOption> QuestionOptions { get; set; }
        public ICollection<QuestionTag> QuestionTags { get; set; }
        public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; }
        public ICollection<StudentAnswer> StudentAnswers { get; set; }
        public ICollection<AIFeedback> AIFeedbacks { get; set; }
    }
}
