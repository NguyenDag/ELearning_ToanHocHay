using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Chatbot
{
    public class ChatConversationDto
    {
        public int ConversationId { get; set; }
        public string? Topic { get; set; }
        public ChatStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class ChatMessageDto
    {
        public long MessageId { get; set; }
        public int ConversationId { get; set; }
        public ChatSender SenderType { get; set; }
        public string Body { get; set; } = "";
        public string? MetadataJson { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class SendChatMessageDto
    {
        public string Text { get; set; } = "";
    }

    public class ChatTurnResultDto
    {
        public int ConversationId { get; set; }
        public ChatMessageDto? Reply { get; set; }
        public bool AiAvailable { get; set; }
        public List<string>? Options { get; set; }
        public ChatStatus ConversationStatus { get; set; }

        /// <summary>The bot has tried enough — offer a human agent.</summary>
        public bool SuggestHuman { get; set; }
    }
}
