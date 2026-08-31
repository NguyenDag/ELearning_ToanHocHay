using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // TẦNG 1 — DANH MỤC (Subject · GradeLevel · CurriculumFramework)
    // Thay toàn bộ "string Subject" và "int GradeLevel [Range(6,9)]".
    // ============================================================

    public enum EducationStage
    {
        Primary,
        LowerSecondary,
        UpperSecondary,
        ExamPrep,
        Other
    }

    [Table("Subject")]
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; }              // "MATH" — unique

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(120)]
        public string Slug { get; set; }

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? IconUrl { get; set; }

        [MaxLength(9)]
        public string? ColorHex { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Course> Courses { get; set; }
        public ICollection<Skill> Skills { get; set; }
        public ICollection<QuestionBank> QuestionBanks { get; set; }
    }

    [Table("GradeLevel")]
    public class GradeLevel
    {
        [Key]
        public int GradeLevelId { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; }              // "G6"

        [Required, MaxLength(100)]
        public string Name { get; set; }              // "Lớp 6"

        public EducationStage Stage { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Course> Courses { get; set; }
        public ICollection<QuestionBank> QuestionBanks { get; set; }
    }

    [Table("CurriculumFramework")]
    public class CurriculumFramework
    {
        [Key]
        public int FrameworkId { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; }              // "KNTT"

        [Required, MaxLength(150)]
        public string Name { get; set; }              // "Kết nối tri thức với cuộc sống"

        [MaxLength(150)]
        public string? Publisher { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Course> Courses { get; set; }
    }
}
