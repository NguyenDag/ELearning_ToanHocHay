using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class CreateQuestionDto
    {
        [Required]
        public string Content { get; set; } // Nội dung câu hỏi
        public int QuestionType { get; set; } = 0; // 0: Trắc nghiệm, 1: Tự luận
        public int Level { get; set; } = 1; // 1: Dễ, 2: TB, 3: Khó

        public List<CreateOptionDto> Options { get; set; } = new List<CreateOptionDto>();
    }

    public class CreateOptionDto
    {
        [Required]
        public string Content { get; set; } // Nội dung đáp án
        public bool IsCorrect { get; set; } = false;
    }
}