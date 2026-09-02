using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Catalog
{
    // ---------- Subject ----------
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? ColorHex { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class SubjectRequestDto
    {
        [Required, MaxLength(20)] public string Code { get; set; } = "";
        [Required, MaxLength(100)] public string Name { get; set; } = "";
        [Required, MaxLength(120)] public string Slug { get; set; } = "";
        public string? Description { get; set; }
        [MaxLength(500)] public string? IconUrl { get; set; }
        [MaxLength(9)] public string? ColorHex { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ---------- GradeLevel ----------
    public class GradeLevelDto
    {
        public int GradeLevelId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public EducationStage Stage { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class GradeLevelRequestDto
    {
        [Required, MaxLength(20)] public string Code { get; set; } = "";
        [Required, MaxLength(100)] public string Name { get; set; } = "";
        public EducationStage Stage { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ---------- CurriculumFramework ----------
    public class FrameworkDto
    {
        public int FrameworkId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Publisher { get; set; }
        public bool IsActive { get; set; }
    }

    public class FrameworkRequestDto
    {
        [Required, MaxLength(20)] public string Code { get; set; } = "";
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        [MaxLength(150)] public string? Publisher { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
