using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Lesson
{
    public class CreateLessonDto
    {
        [Required(ErrorMessage = "TopicId là bắt buộc")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Tên lesson là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên lesson không được vượt quá 200 ký tự")]
        public string LessonName { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Thời lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải lớn hơn 0")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "OrderIndex là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderIndex phải lớn hơn 0")]
        public int OrderIndex { get; set; }

        public bool IsFree { get; set; } = true;
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Status là bắt buộc")]
        public LessonStatus Status { get; set; } = LessonStatus.Published;

        [Required(ErrorMessage = "CreatedBy là bắt buộc")]
        public int CreatedBy { get; set; }
    }
}
