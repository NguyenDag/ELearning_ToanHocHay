using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ExerciseRequestDto
    {
        public int? TopicId { get; set; }
        public int? ChapterId { get; set; }
        public string ExerciseName { get; set; }
        public ExerciseType ExerciseType { get; set; }
        public int TotalQuestions { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsFree { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public double TotalScores { get; set; }
        public double PassingScore { get; set; }
        public ExerciseStatus Status { get; set; }
    }
}
