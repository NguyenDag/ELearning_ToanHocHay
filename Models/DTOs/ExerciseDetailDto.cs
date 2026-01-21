namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ExerciseDetailDto
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public int? DurationMinutes { get; set; }
        // Sử dụng QuestionDto đã định nghĩa bên dưới
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }

}