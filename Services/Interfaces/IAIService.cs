using ELearning_ToanHocHay_Control.Models.DTOs.Chatbot;
using ELearning_ToanHocHay_Control.Services.Implementations;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateHintAsync(string prompt);
        Task<string> GenerateFeedbackAsync(string prompt);

        // Structured AI Methods (for integration with AIHintService/AIFeedbackService)
        Task<HintResponse?> GenerateHintStructuredAsync(HintRequest request);
        Task<FeedbackResponse?> GenerateFeedbackStructuredAsync(FeedbackRequest request);

        // Chatbot Methods
        Task<ChatbotResponse?> SendChatbotMessageAsync(ChatbotMessageRequest request);
        Task<ChatbotResponse?> SendChatbotQuickReplyAsync(ChatbotQuickReplyRequest request);
    }
}
