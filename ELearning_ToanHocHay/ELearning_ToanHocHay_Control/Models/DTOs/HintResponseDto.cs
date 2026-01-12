namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class HintResponseDto
    {
        public int Level { get; set; }
        public string HintText { get; set; } = null!;
        public bool IsMaxHintReached { get; set; }
    }
}
