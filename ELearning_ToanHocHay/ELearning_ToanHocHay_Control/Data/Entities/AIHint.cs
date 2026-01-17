using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("AIHint")]
    public class AIHint
    {
        [Key]
        public int HintId { get; set; }

        [ForeignKey("Attempt")]
        public int AttemptId { get; set; }

        [ForeignKey("Question")]
        public int QuestionId { get; set; }

        //[Column(TypeName = "nvarchar(max)")]
        public string? HintText { get; set; }

        public int HintLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ExerciseAttempt? Attempt { get; set; }
        public Question? Question { get; set; }
    }
}
