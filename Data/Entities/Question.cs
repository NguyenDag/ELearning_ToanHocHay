using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum QuestionType
    {
        MultipleChoice = 0,
        TrueFalse = 1,
        FillBlank = 2,
        Essay = 3
    }

    // (Có thể giữ hoặc bỏ Enum DifficultyLevel nếu chuyển sang dùng int Level)
    public enum DifficultyLevel
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public enum QuestionStatus
    {
        Draft,
        PendingReview,
        Approved,
        Rejected
    }

    [Table("Question")] // Đảm bảo tên bảng khớp với SQL đã chạy
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        // Nếu hệ thống của bạn chưa có bảng QuestionBank, có thể để nullable hoặc comment lại nếu lỗi
        public int? BankId { get; set; }

        public required string QuestionText { get; set; }

        [MaxLength(500)]
        public string? QuestionImageUrl { get; set; }

        public required QuestionType QuestionType { get; set; }

        // --- SỬA ĐỔI QUAN TRỌNG ĐỂ HẾT LỖI ---

        // 1. Đổi tên thành Level (kiểu int) để khớp với code Service & SQL
        public int Level { get; set; } = 1;

        // 2. Thêm TotalScores vì Service có dùng
        public double TotalScores { get; set; } = 1;

        // -------------------------------------

        // Đáp án đúng (dùng cho TrueFalse, FillInBlank)
        public string? CorrectAnswer { get; set; }

        public string? Explanation { get; set; }

        public QuestionStatus Status { get; set; } = QuestionStatus.Approved;
        public bool IsActive { get; set; } = true;

        public int CreatedBy { get; set; }
        public int? ReviewedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? RejectReason { get; set; }
        public DateTime? PublishedAt { get; set; }

        public int Version { get; set; } = 1;

        // Navigation Properties
        public QuestionBank? QuestionBank { get; set; }
        public User? Creator { get; set; }
        public User? Reviewer { get; set; }

        // Danh sách đáp án
        public ICollection<QuestionOption> QuestionOptions { get; set; }

        public ICollection<QuestionTag> QuestionTags { get; set; }
        public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; }
        public ICollection<StudentAnswer> StudentAnswers { get; set; }
        public ICollection<AIFeedback> AIFeedbacks { get; set; }
    }
}