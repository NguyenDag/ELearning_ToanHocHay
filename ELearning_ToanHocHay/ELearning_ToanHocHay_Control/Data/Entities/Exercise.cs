using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum ExerciseType
    {
        Practice,
        Quiz,
        Test,
        Exam
    }

    public enum ExerciseStatus
    {
        Draft,
        Published,
        Archived
    }

    [Table("Exercise")]
    public class Exercise
    {
        [Key]
        public int ExerciseId { get; set; }

        public int? TopicId { get; set; }
        public int? ChapterId { get; set; }

        [Required, MaxLength(255)]
        public string ExerciseName { get; set; }

        public ExerciseType ExerciseType { get; set; }

        public int TotalQuestions { get; set; }

        public int? DurationMinutes { get; set; }

        public bool IsFree { get; set; } = false;

        public bool IsActive { get; set; } = false;

        public double TotalPoints { get; set; }
        
        public double PassingScore { get; set; }


        public ExerciseStatus Status { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Topic? Topic { get; set; }
        public Chapter? Chapter { get; set; }
        public User? Creator { get; set; }

        public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; }
        public ICollection<ExerciseAttempt>? ExerciseAttempts { get; set; }
    }
}
