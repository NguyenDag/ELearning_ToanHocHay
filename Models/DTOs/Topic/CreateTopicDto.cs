using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Topic
{
    public class CreateTopicDto
    {
        [Required(ErrorMessage = "ChapterId là bắt buộc")]
        public int ChapterId { get; set; }

        [Required(ErrorMessage = "Tên topic là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên topic không được vượt quá 200 ký tự")]
        public string TopicName { get; set; }

        [Required(ErrorMessage = "OrderIndex là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderIndex phải lớn hơn 0")]
        public int OrderIndex { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        public bool IsFree { get; set; } = true;
    }
}
