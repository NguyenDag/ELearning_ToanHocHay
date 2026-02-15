namespace ELearning_ToanHocHay_Control.Models.DTOs.Topic
{
    public class UpsertTopicDto
    {
        public int? TopicId { get; set; }

        // Chỉ bắt buộc khi tạo mới
        public int? ChapterId { get; set; }
        public string? TopicName { get; set; }
        public string? Description { get; set; }
        public bool IsFree { get; set; }
        public int OrderIndex { get; set; }
    }
}
