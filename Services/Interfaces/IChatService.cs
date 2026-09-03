using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Chatbot;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>P6 — chatbot conversations persisted on the C# side (§5.12 / A5).</summary>
    public interface IChatService
    {
        Task<ApiResponse<ChatTurnResultDto>> SendUserTurnAsync(int userId, int? studentId, string text, bool isQuickReply);
        Task<ApiResponse<List<ChatConversationDto>>> GetMyConversationsAsync(int userId);
        Task<ApiResponse<List<ChatMessageDto>>> GetMessagesAsync(int userId, int conversationId);
    }
}
