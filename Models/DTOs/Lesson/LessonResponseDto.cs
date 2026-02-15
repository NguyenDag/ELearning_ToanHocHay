namespace ELearning_ToanHocHay_Control.Models.DTOs.Lesson
{
    public class LessonResponseDto
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public string LessonName { get; set; }
        public string Description { get; set; }
        public int DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
