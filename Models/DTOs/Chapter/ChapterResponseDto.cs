namespace ELearning_ToanHocHay_Control.Models.DTOs.Chapter
{
    public class ChapterResponseDto
    {
        public int Id { get; set; }
        public int CurriculumId { get; set; }
        public string CurriculumName { get; set; }
        public string ChapterName { get; set; }
        public int OrderIndex { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
