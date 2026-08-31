using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // CẮT NGANG — TIẾN ĐỘ
    // StudentCourse (ghi danh nhiều khoá) · NodeProgress (gộp StudentProgress
    // + LessonProgress) · SkillProgress · DailyActivitySnapshot
    // ============================================================

    public enum MasteryLevel
    {
        NotStarted,
        Beginner,
        Intermediate,
        Advanced,
        Mastered
    }

    public enum ProgressStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    public enum EnrollSource
    {
        Self,
        Assigned,
        Subscription,
        Purchase
    }

    public enum StudentCourseStatus
    {
        Active,
        Completed,
        Expired,
        Cancelled
    }

    [Table("StudentCourse")]
    public class StudentCourse
    {
        [Key]
        public int StudentCourseId { get; set; }

        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int CourseVersionId { get; set; }      // phiên bản đã ghi danh

        public EnrollSource Source { get; set; }

        public StudentCourseStatus Status { get; set; } = StudentCourseStatus.Active;

        [Column(TypeName = "decimal(5,2)")]
        public decimal ProgressPercent { get; set; }  // cache

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? AccessExpiresAt { get; set; }

        // Navigation
        public Student? Student { get; set; }
        public Course? Course { get; set; }
        public CourseVersion? CourseVersion { get; set; }
    }

    [Table("NodeProgress")]
    public class NodeProgress
    {
        [Key]
        public int NodeProgressId { get; set; }

        public int StudentId { get; set; }
        public int NodeId { get; set; }               // chương / topic / lesson đều được

        public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;
        public MasteryLevel MasteryLevel { get; set; } = MasteryLevel.NotStarted;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CompletionPercent { get; set; }

        public int TimeSpentSeconds { get; set; }
        public int TotalAttempts { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }

        public string? CommonMistakesJson { get; set; }

        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Student? Student { get; set; }
        public ContentNode? Node { get; set; }
    }

    [Table("SkillProgress")]
    public class SkillProgress
    {
        [Key]
        public int SkillProgressId { get; set; }

        public int StudentId { get; set; }
        public int SkillId { get; set; }

        [Column(TypeName = "decimal(4,3)")]
        public decimal MasteryScore { get; set; }     // 0–1

        public DateTime? LastAssessedAt { get; set; }

        // Navigation
        public Student? Student { get; set; }
        public Skill? Skill { get; set; }
    }

    // Dữ liệu cho heatmap + streak (thay tính lặp từ toàn bộ attempt).
    [Table("DailyActivitySnapshot")]
    public class DailyActivitySnapshot
    {
        public int StudentId { get; set; }
        public DateOnly Date { get; set; }

        public int MinutesStudied { get; set; }
        public int ExercisesDone { get; set; }
        public int LessonsDone { get; set; }
        public int QuestionsAnswered { get; set; }

        // Navigation
        public Student? Student { get; set; }
    }
}
