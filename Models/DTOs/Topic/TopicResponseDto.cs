namespace ELearning_ToanHocHay_Control.Models.DTOs.Topic
{
    public class TopicResponseDto
    {
        public int Id { get; set; }
        public int ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string TopicName { get; set; }
        public int OrderIndex { get; set; }
        public string Description { get; set; }
        public bool IsFree { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
