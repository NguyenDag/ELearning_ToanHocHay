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

        // text (FillBlank, Essay)
        public string? AnswerText { get; set; }

        // MultipleChoice, TrueFalse
        public int? SelectedOptionId { get; set; }
        public bool IsCorrect { get; set; } = false;

        // Essay answers wait for a human to grade them.
        public bool NeedsManualGrading { get; set; } = false;

        public double PointsEarned { get; set; } = 0;

        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ExerciseAttempt? ExerciseAttempt { get; set; }
        public Question? Question { get; set; }
        public QuestionOption? SelectedOption { get; set; }
    }
}
