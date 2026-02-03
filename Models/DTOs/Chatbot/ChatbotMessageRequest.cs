using System.Text.Json.Serialization;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Chatbot
{
    public class ChatbotMessageRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        // Optional: FE có thể gửi UserId nếu muốn (nhưng Controller sẽ ưu tiên lấy từ Token)
        [JsonPropertyName("UserId")]
        public string? UserId { get; set; }
    }
}
