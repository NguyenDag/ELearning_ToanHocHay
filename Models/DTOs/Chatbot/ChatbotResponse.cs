using System.Text.Json.Serialization;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Chatbot
{
    public class ChatbotResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("UserId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public ChatbotResponseData? Response { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class ChatbotResponseData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "message"; // "message", "quick_reply", "form"

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<string>? Options { get; set; }

        [JsonPropertyName("form_fields")]
        public List<string>? FormFields { get; set; }
    }
}
