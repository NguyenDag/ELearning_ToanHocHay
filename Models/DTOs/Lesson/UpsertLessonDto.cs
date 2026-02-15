using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Lesson
{
    public class UpsertLessonDto
    {
        public int? LessonId { get; set; }

        // Chỉ bắt buộc khi tạo mới
        public string? LessonName { get; set; }
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }    
        public int OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public bool IsActive { get; set; }
        public LessonStatus Status { get; set; }
        public int CreatedBy { get; set; }
    }
}
