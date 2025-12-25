using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum MasteryLevel
    {
        NotStarted,
        Beginner,
        Intermediate,
        Advanced,
        Mastered
    }

    [Table("StudentProgress")]
    public class StudentProgress
    {
        [Key]
        public int ProgressId { get; set; }

        public int StudentId { get; set; }
        public int TopicId { get; set; }

        public MasteryLevel MasteryLevel { get; set; }

        public int TotalAttempts { get; set; } = 0;
        public int CorrectCount { get; set; } = 0;
        public int WrongCount { get; set; } = 0;

        public DateTime? LastPracticed { get; set; }

        public string? CommonMistakesJson { get; set; }

        // Navigation
        public Student? Student { get; set; }
        public Topic? Topic { get; set; }
    }
}
