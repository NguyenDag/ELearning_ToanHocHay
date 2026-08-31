using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum AttemptStatus
    {
        InProgress,
        Submitted,
        Timeout
    }

    [Table("ExerciseAttempt")]
    public class ExerciseAttempt
    {
        // NOTE: doc §12.3 khuyến nghị bigint cho bảng này — hoãn lại để giảm ripple ở app layer.
        [Key]
        public int AttemptId { get; set; }

        // §5.14 — cho phép khách làm bài: đúng 1 trong 2 được set (CHECK).
        public int? StudentId { get; set; }
        public Guid? GuestSessionId { get; set; }

        public int ExerciseId { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime PlannedEndTime { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

        public double TotalScore { get; set; } = 0;
        public double MaxScore { get; set; }

        public decimal CompletionPercentage { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public int WrongAnswers { get; set; } = 0;

        // Navigation
        public Student? Student { get; set; }
        public GuestSession? GuestSession { get; set; }
        public Exercise? Exercise { get; set; }

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
        public ICollection<AIFeedback> AIFeedbacks { get; set; }
        public ICollection<AIHint> AIHints { get; set; }
    }
}
