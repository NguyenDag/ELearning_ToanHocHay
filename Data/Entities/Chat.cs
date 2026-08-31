using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // HỖ TRỢ: CHAT REALTIME + AI (§5.12)
    // DB chỉ lưu hội thoại; realtime = SignalR/WebSocket (hạ tầng).
    // ============================================================

    public enum ChatStatus
    {
        Bot,
        WaitingAgent,
        WithAgent,
        EscalatedToPhone,
        Closed
    }

    public enum ChatSender
    {
        User,
        AI,
        Staff,
        System
    }

    [Table("ChatConversation")]
    public class ChatConversation
    {
        [Key]
        public int ConversationId { get; set; }

        public int InitiatorUserId { get; set; }
        public int? StudentId { get; set; }

        [MaxLength(255)]
        public string? Topic { get; set; }

        public ChatStatus Status { get; set; } = ChatStatus.Bot;

        public int? AssignedStaffId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        // Navigation
        public User? Initiator { get; set; }
        public Student? Student { get; set; }
        public User? AssignedStaff { get; set; }
        public ICollection<ChatMessage> Messages { get; set; }
    }

    [Table("ChatMessage")]
    public class ChatMessage
    {
        [Key]
        public long MessageId { get; set; }

        public int ConversationId { get; set; }

        public ChatSender SenderType { get; set; }
        public int? SenderUserId { get; set; }

        [Required]
        public string Body { get; set; }

        public string? MetadataJson { get; set; }     // AI: model, confidence, nguồn

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ChatConversation? Conversation { get; set; }
        public User? Sender { get; set; }
    }
}
