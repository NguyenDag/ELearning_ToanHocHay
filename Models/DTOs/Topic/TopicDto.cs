using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Models.DTOs.Lesson;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Topic
{
    public class TopicDto
    {
        public int TopicId { get; set; }
        public int ChapterId { get; set; }
        public string TopicName { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
        public List<LessonDto> Lessons { get; set; } = new();
    }
}
