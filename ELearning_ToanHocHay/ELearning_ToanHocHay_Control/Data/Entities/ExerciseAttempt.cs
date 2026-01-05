using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("ExerciseAttempt")]
    public class ExerciseAttempt
    {
        [Key]
        public int AttemptId { get; set; }

        public int StudentId { get; set; }
        public int? ExerciseId { get; set; }

        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }

        public int TotalScore { get; set; } = 0;
        public int MaxScore { get; set; }

        public decimal CompletionPercentage { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public int WrongAnswers { get; set; } = 0;

        // Navigation
        public Student? Student { get; set; }
        public Exercise? Exercise { get; set; }

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
        public ICollection<AIFeedback> AIFeedbacks { get; set; }
    }
}
