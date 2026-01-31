using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ChapterDto
    {
        public int ChapterId { get; set; }
        public int CurriculumId { get; set; }
        public string ChapterName { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<TopicDto> Topics { get; set; } = new();
    }
    public class CreateChapterDto
    {
        [Required]
        public int CurriculumId { get; set; }

        [Required, MaxLength(255)]
        public string ChapterName { get; set; }

        public int OrderIndex { get; set; }
        public string? Description { get; set; }
    }
    public class UpdateChapterDto
    {
        [Required, MaxLength(255)]
        public string ChapterName { get; set; }

        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

}
