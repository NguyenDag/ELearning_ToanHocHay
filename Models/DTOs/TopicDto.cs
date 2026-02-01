using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs
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

    public class CreateTopicDto
    {
        [Required]
        public int ChapterId { get; set; }

        [Required, MaxLength(255)]
        public string TopicName { get; set; }

        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsFree { get; set; }
    }

    public class UpdateTopicDto
    {
        [Required, MaxLength(255)]
        public string TopicName { get; set; }

        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
    }
}
