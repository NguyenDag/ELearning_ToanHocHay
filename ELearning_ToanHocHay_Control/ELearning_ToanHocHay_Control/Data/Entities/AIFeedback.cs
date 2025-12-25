using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("AIFeedback")]
    public class AIFeedback
    {
        [Key]
        public int FeedbackId { get; set; }

        public int AttemptId { get; set; }
        public int QuestionId { get; set; }

        public string? HintText { get; set; }
        public string? FeedbackText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ExerciseAttempt? Attempt { get; set; }
        public Question? Question { get; set; }
    }
}
