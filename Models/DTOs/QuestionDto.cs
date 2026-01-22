using System.Text.Json.Serialization;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public QuestionType QuestionType { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public List<QuestionOptionDto>? Options { get; set; }
    }
}