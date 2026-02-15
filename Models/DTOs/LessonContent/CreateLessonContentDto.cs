using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.LessonContent
{
    public class CreateLessonContentDto
    {
        [Required(ErrorMessage = "LessonId là bắt buộc")]
        public int LessonId { get; set; }

        [Required(ErrorMessage = "BlockType là bắt buộc")]
        public LessonBlockType BlockType { get; set; }

        public string? ContentText { get; set; }

        [StringLength(500, ErrorMessage = "URL không được vượt quá 500 ký tự")]
        public string? ContentUrl { get; set; }

        [Required(ErrorMessage = "OrderIndex là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderIndex phải lớn hơn 0")]
        public int OrderIndex { get; set; }
    }
}
