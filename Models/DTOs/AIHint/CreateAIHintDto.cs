namespace ELearning_ToanHocHay_Control.Models.DTOs.AIHint
{
    public class CreateAIHintDto
    {
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? HintText { get; set; }
        public int HintLevel { get; set; }
    }
}
