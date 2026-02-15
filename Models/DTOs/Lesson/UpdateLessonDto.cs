using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Lesson
{
    public class UpdateLessonDto
    {
        [Required, MaxLength(255)]
        public string LessonName { get; set; }

        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
    }
}
