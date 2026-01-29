using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class CreateQuestionDto
    {
        [Required]
        public int BankId { get; set; }
        [Required]
        public string QuestionText { get; set; } // Nội dung câu hỏi
        public string? QuestionImageUrl { get; set; }
        public QuestionType QuestionType { get; set; } = 0; // 0: Trắc nghiệm, 1: Tự luận
        public DifficultyLevel DifficultyLevel { get; set; } = 0; // 0: Dễ, 1: TB, 2: Khó
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }

        public List<CreateQuestionOptionDto> Options { get; set; } = new List<CreateQuestionOptionDto>();
    }

    public class CreateQuestionOptionDto
    {
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }
}