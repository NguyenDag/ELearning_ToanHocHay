using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ExerciseDto
    {
        public int ExerciseId { get; set; }
        public int? TopicId { get; set; }
        public int? ChapterId { get; set; }
        public string ExerciseName { get; set; }
        public ExerciseType ExerciseType { get; set; }
        public int TotalQuestions { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsFree { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public double TotalPoints { get; set; }
        public double PassingScore { get; set; }
        public ExerciseStatus Status { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- THÊM DÒNG NÀY ---
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }
}