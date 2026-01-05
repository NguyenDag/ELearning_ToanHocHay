using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum CurriculumStatus
    {
        Draft,
        Published,
        Archived
    }

    [Table("Curriculum")]
    public class Curriculum
    {
        [Key]
        public int CurriculumId { get; set; }

        [Range(6, 9)]
        public int GradeLevel { get; set; }

        [Required, MaxLength(100)]
        public string Subject { get; set; }

        [Required, MaxLength(255)]
        public string CurriculumName { get; set; }

        public string? Description { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int Version { get; set; } = 1;

        public CurriculumStatus Status { get; set; }

        // Navigation
        public User? Creator { get; set; }
        public ICollection<Chapter> Chapters { get; set; }
    }
}
