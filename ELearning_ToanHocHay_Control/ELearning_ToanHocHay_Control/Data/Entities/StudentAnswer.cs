using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("StudentAnswer")]
    public class StudentAnswer
    {
        [Key]
        public int AnswerId { get; set; }

        public int AttemptId { get; set; }
        public int QuestionId { get; set; }

        public string? AnswerText { get; set; }

        public bool? IsCorrect { get; set; }

        public int PointsEarned { get; set; } = 0;

        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ExerciseAttempt? Attempt { get; set; }
        public Question? Question { get; set; }
    }
}
