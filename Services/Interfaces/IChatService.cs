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

        // ----- escalation -----
        Task<ApiResponse<ChatConversationDto>> RequestHumanAsync(int userId);
        Task<ApiResponse<bool>> CloseAsync(int userId, int conversationId, bool isStaff);

        // staff-side (SupportStaff / admin)
        Task<ApiResponse<List<ChatConversationDto>>> GetQueueAsync();
        Task<ApiResponse<ChatConversationDto>> AssignToMeAsync(int staffUserId, int conversationId);
        Task<ApiResponse<ChatMessageDto>> StaffReplyAsync(int staffUserId, int conversationId, string text, bool isAdmin);
    }
}
