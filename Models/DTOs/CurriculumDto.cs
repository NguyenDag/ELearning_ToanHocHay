using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class CurriculumDto
    {
        public int CurriculumId { get; set; }

        public int GradeLevel { get; set; }

        public string Subject { get; set; } = null!;

        public string CurriculumName { get; set; } = null!;

        public string? Description { get; set; }

        public CurriculumStatus Status { get; set; }

        public int Version { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
    public class CreateCurriculumDto
    {
        [Required]
        [Range(6, 9)]
        public int GradeLevel { get; set; }

        [Required, MaxLength(100)]
        public string Subject { get; set; } = null!;

        [Required, MaxLength(255)]
        public string CurriculumName { get; set; } = null!;

        public string? Description { get; set; }

        // Mặc định khi tạo
        public CurriculumStatus Status { get; set; } = CurriculumStatus.Draft;
    }
    public class UpdateCurriculumDto
    {
        [Required]
        [Range(6, 9)]
        public int GradeLevel { get; set; }

        [Required, MaxLength(100)]
        public string Subject { get; set; } = null!;

        [Required, MaxLength(255)]
        public string CurriculumName { get; set; } = null!;

        public string? Description { get; set; }

        public CurriculumStatus Status { get; set; }
    }
}
