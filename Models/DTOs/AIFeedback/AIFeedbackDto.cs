namespace ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback
{
    public class AIFeedbackDto
    {
        public int FeedbackId { get; set; }
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? FullSolution { get; set; }
        public string? MistakeAnalysis { get; set; }
        public string? ImprovementAdvice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
