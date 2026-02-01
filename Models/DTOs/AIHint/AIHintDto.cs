namespace ELearning_ToanHocHay_Control.Models.DTOs.AIHint
{
    public class AIHintDto
    {
        public int HintId { get; set; }
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? HintText { get; set; }
        public int HintLevel { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
