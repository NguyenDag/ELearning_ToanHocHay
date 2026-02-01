using System.Text.Json.Serialization;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Chatbot
{
    public class ChatbotQuickReplyRequest
    {
        [JsonPropertyName("reply")]
        public string Reply { get; set; } = string.Empty;

        [JsonPropertyName("UserId")]
        public string? UserId { get; set; }
    }
}
