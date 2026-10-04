namespace ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback
{
    public class CreateAIFeedbackDto
    {
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? StudentAnswer { get; set; }
        public string? FullSolution { get; set; }
        public string? MistakeAnalysis { get; set; }
        public string? ImprovementAdvice { get; set; }

        /// <summary>
        /// Set by internal background jobs to bypass the per-student AI quota gate.
        /// Never accepted from external clients (not bound from JSON).
        /// </summary>
        internal bool BypassQuota { get; set; }
    }
}
