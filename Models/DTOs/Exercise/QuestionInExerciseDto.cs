using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Exercise
{
    /// <summary>A question as it sits inside an exercise (order + per-question score).</summary>
    public class QuestionInExerciseDto
    {
        public int QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public double Score { get; set; }
        public string QuestionText { get; set; } = "";
        public string? QuestionImageUrl { get; set; }
        public QuestionType QuestionType { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }
}
