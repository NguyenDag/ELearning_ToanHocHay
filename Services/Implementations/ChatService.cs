using System.Text.Json;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Chatbot;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IAIService _aiService;
        private readonly IAiQuotaService _quota;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            AppDbContext context, IAIService aiService, IAiQuotaService quota, ILogger<ChatService> logger)
        {
            _context = context;
            _aiService = aiService;
            _quota = quota;
            _logger = logger;
        }

        public async Task<ApiResponse<ChatTurnResultDto>> SendUserTurnAsync(
            int userId, int? studentId, string text, bool isQuickReply)
        {
            if (string.IsNullOrWhiteSpace(text))
                return ApiResponse<ChatTurnResultDto>.ErrorResponse("Nội dung không được để trống");

            var conversation = await GetOrCreateOpenConversationAsync(userId, studentId);

            var now = DateTime.UtcNow;
            _context.ChatMessages.Add(new ChatMessage
            {
                ConversationId = conversation.ConversationId,
                SenderType = ChatSender.User,
                SenderUserId = userId,
                Body = text.Trim(),
                SentAt = now
            });
            await _context.SaveChangesAsync();

            // Call Flask. On any failure we still persist a System message and return 200.
            ChatbotResponse? ai = null;
            try
            {
                ai = isQuickReply
                    ? await _aiService.SendChatbotQuickReplyAsync(new ChatbotQuickReplyRequest { Reply = text, UserId = userId.ToString() })
                    : await _aiService.SendChatbotMessageAsync(new ChatbotMessageRequest { Text = text, UserId = userId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chatbot call failed for user {UserId}", userId);
            }

            var aiOk = ai is { Success: true, Response: not null }
                       && !string.IsNullOrWhiteSpace(ai.Response!.Message);
            var replyBody = aiOk
                ? ai!.Response!.Message
                : "Trợ lý AI hiện chưa phản hồi được. Bạn thử lại sau ít phút nhé.";

            var reply = new ChatMessage
            {
                ConversationId = conversation.ConversationId,
                SenderType = aiOk ? ChatSender.AI : ChatSender.System,
                Body = replyBody,
                MetadataJson = aiOk ? JsonSerializer.Serialize(new { type = ai!.Response!.Type }) : null,
                SentAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(reply);
            await _context.SaveChangesAsync();

            if (aiOk && studentId is int sid)
            {
                try { await _quota.RecordChatAsync(sid); } catch { /* best effort */ }
            }

            return ApiResponse<ChatTurnResultDto>.SuccessResponse(new ChatTurnResultDto
            {
                ConversationId = conversation.ConversationId,
                AiAvailable = aiOk,
                Options = aiOk ? ai!.Response!.Options : null,
                Reply = Map(reply)
            });
        }

        public async Task<ApiResponse<List<ChatConversationDto>>> GetMyConversationsAsync(int userId)
        {
            var items = await _context.ChatConversations
                .AsNoTracking()
                .Where(c => c.InitiatorUserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ChatConversationDto
                {
                    ConversationId = c.ConversationId,
                    Topic = c.Topic,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    ClosedAt = c.ClosedAt,
                    MessageCount = c.Messages.Count
                })
                .ToListAsync();
            return ApiResponse<List<ChatConversationDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<List<ChatMessageDto>>> GetMessagesAsync(int userId, int conversationId)
        {
            var owns = await _context.ChatConversations
                .AnyAsync(c => c.ConversationId == conversationId && c.InitiatorUserId == userId);
            if (!owns) return ApiResponse<List<ChatMessageDto>>.ErrorResponse("Conversation not found");

            var msgs = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Select(m => Map(m))
                .ToListAsync();
            return ApiResponse<List<ChatMessageDto>>.SuccessResponse(msgs);
        }

        private async Task<ChatConversation> GetOrCreateOpenConversationAsync(int userId, int? studentId)
        {
            var open = await _context.ChatConversations
                .Where(c => c.InitiatorUserId == userId && c.Status != ChatStatus.Closed)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
            if (open != null) return open;

            var conv = new ChatConversation
            {
                InitiatorUserId = userId,
                StudentId = studentId,
                Status = ChatStatus.Bot,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conv);
            await _context.SaveChangesAsync();
            return conv;
        }

        private static ChatMessageDto Map(ChatMessage m) => new()
        {
            MessageId = m.MessageId,
            ConversationId = m.ConversationId,
            SenderType = m.SenderType,
            Body = m.Body,
            MetadataJson = m.MetadataJson,
            SentAt = m.SentAt
        };
    }
}
