using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Chapter
{
    public class UpdateChapterDto
    {
        [Required, MaxLength(255)]
        public string ChapterName { get; set; }

        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
