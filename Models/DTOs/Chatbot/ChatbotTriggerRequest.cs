using System.Text.Json.Serialization;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Chatbot
{
    public class ChatbotTriggerRequest
    {
        [JsonPropertyName("trigger")]
        public string Trigger { get; set; } = string.Empty;

        [JsonPropertyName("UserId")]
        public string? UserId { get; set; }
    }
}
