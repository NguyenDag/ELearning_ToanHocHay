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

    public enum AccessTier
    {
        Free,
        Standard,
        Premium
    }

    [Table("Exercise")]
    public class Exercise
    {
        [Key]
        public int ExerciseId { get; set; }

        // §5.5 — một FK NodeId thay TopicId?/ChapterId? (xoá hack ClientSetNull).
        // Nullable: bài luyện tập/ngẫu nhiên có thể không gắn node cụ thể.
        public int? NodeId { get; set; }

        [Required, MaxLength(255)]
        public string ExerciseName { get; set; }

        public ExerciseType ExerciseType { get; set; }

        public int TotalQuestions { get; set; }

        public int? DurationMinutes { get; set; }

        public int? MaxAttempts { get; set; }

        public bool IsFree { get; set; } = false;

        public AccessTier RequiredTier { get; set; } = AccessTier.Free;

        public bool IsActive { get; set; } = false;

        public double TotalScores { get; set; }
        public double PassingScore { get; set; }

        public ExerciseStatus Status { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ContentNode? Node { get; set; }
        public User? Creator { get; set; }

        public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; }
        public ICollection<ExerciseAttempt>? ExerciseAttempts { get; set; }
    }
}
