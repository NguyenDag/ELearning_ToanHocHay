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
        [Key]
        public int AttemptId { get; set; }

        public int StudentId { get; set; }
        public int ExerciseId { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        // Thời điểm PHẢI kết thúc
        public DateTime PlannedEndTime { get; set; }

        // Thời điểm thực sự submit
        public DateTime? SubmittedAt { get; set; }

        public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

        public double TotalScore { get; set; } = 0;
        public double MaxScore { get; set; }

        public decimal CompletionPercentage { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public int WrongAnswers { get; set; } = 0;

        // Navigation
        public Student? Student { get; set; }
        public Exercise? Exercise { get; set; }

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
        public ICollection<AIFeedback> AIFeedbacks { get; set; }
        public ICollection<AIHint> AIHints { get; set; }
    }
}
