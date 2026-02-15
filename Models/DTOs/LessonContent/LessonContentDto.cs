using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.LessonContent
{

    public class LessonContentDto
    {
        public int ContentId { get; set; }
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
    }
}
