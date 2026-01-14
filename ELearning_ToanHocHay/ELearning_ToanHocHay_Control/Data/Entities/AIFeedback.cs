using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("AIFeedback")]
    public class AIFeedback
    {
        [Key]
        public int FeedbackId { get; set; }

        [ForeignKey("Attempt")]
        public int AttemptId { get; set; }

        [ForeignKey("Question")]
        public int QuestionId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? FullSolution { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? MistakeAnalysis { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ImprovementAdvice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ExerciseAttempt? Attempt { get; set; }
        public Question? Question { get; set; }
    }
}
