using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Chapter
{
    public class CreateChapterDto
    {
        [Required(ErrorMessage = "CurriculumId là bắt buộc")]
        public int CurriculumId { get; set; }

        [Required(ErrorMessage = "Tên chương là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên chương không được vượt quá 200 ký tự")]
        public string ChapterName { get; set; }

        [Required(ErrorMessage = "OrderIndex là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderIndex phải lớn hơn 0")]
        public int OrderIndex { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }
    }
}
