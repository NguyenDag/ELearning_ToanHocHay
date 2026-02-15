namespace ELearning_ToanHocHay_Control.Models.DTOs.LessonContent
{
    public class LessonContentResponseDto
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string BlockType { get; set; }
        public string ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
    }
}
