using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.LessonContent;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Lesson
{
    public class LessonDto
    {
        public int LessonId { get; set; }
        public int TopicId { get; set; }
        public string LessonName { get; set; }
        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
        public LessonStatus Status { get; set; }

        public List<LessonContentDto> Contents { get; set; } = new();
    }
}
