using System.Text.Json.Serialization;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class QuestionDto
    {
        public int QuestionId { get; set; }

        [JsonPropertyName("questionText")]
        public string Content { get; set; }

        public List<OptionDto> Options { get; set; } = new List<OptionDto>();
    }

    public class OptionDto
    {
        public int OptionId { get; set; }

        [JsonPropertyName("optionText")]
        public string Content { get; set; }

        public bool IsCorrect { get; set; }
    }
}