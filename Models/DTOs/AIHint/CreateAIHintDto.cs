namespace ELearning_ToanHocHay_Control.Models.DTOs.AIHint
{
    public class CreateAIHintDto
    {
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        
        /// <summary>
        /// Optional: Nếu null, hệ thống sẽ tự động gọi AI để sinh hint
        /// </summary>
        public string? HintText { get; set; }
        
        public int HintLevel { get; set; } = 1;
        
        /// <summary>
        /// Câu trả lời của học sinh (cần cho AI để sinh hint phù hợp)
        /// </summary>
        public string? StudentAnswer { get; set; }
    }
}
