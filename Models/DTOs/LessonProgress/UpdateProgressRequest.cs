using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.LessonProgress
{
    public class UpdateProgressRequest
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Range(0, int.MaxValue)]
        public int WatchTime { get; set; }
    }
}
