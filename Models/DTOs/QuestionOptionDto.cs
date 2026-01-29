namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class QuestionOptionDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }
    }
}
