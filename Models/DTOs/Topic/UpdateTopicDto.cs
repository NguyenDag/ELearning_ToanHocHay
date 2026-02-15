using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Topic
{
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
