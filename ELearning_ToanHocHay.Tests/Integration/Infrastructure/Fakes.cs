using System.Text.RegularExpressions;
using ELearning_ToanHocHay_Control.Models.DTOs.AI;
using ELearning_ToanHocHay_Control.Models.DTOs.Chatbot;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

/// <summary>Thay Flask AI. Mặc định "khoẻ" và trả nội dung định sẵn; đếm số lần gọi.</summary>
public sealed class FakeAiService : IAIService
{
    public bool Healthy { get; private set; } = true;
    public bool ThrowTimeout { get; set; }
    public string NextHint { get; set; } = "Gợi ý mẫu.";
    public string NextFeedback { get; set; } = "Nhận xét mẫu.";
    public int HintCalls { get; private set; }
    public int FeedbackCalls { get; private set; }

    public void SetHealthy(bool value) => Healthy = value;

    private void GuardTimeout()
    {
        if (ThrowTimeout) throw new TaskCanceledException("FakeAiService: forced timeout");
    }

    public Task<string> GenerateHintAsync(string prompt)
    {
        GuardTimeout();
        HintCalls++;
        return Task.FromResult(NextHint);
    }

    public Task<string> GenerateFeedbackAsync(string prompt)
    {
        GuardTimeout();
        FeedbackCalls++;
        return Task.FromResult(NextFeedback);
    }

    public Task<AIHintResponse?> GenerateHintStructuredAsync(AIHintRequest request)
    {
        GuardTimeout();
        HintCalls++;
        return Task.FromResult<AIHintResponse?>(new AIHintResponse { HintText = NextHint, HintLevel = 1 });
    }

    public Task<AIFeedbackResponse?> GenerateFeedbackStructuredAsync(AIFeedbackRequest request)
    {
        GuardTimeout();
        FeedbackCalls++;
        return Task.FromResult<AIFeedbackResponse?>(new AIFeedbackResponse
        {
            FullSolution = NextFeedback,
            MistakeAnalysis = "—",
            ImprovementAdvice = "—",
        });
    }

    public Task<AIInsightResponse?> GenerateInsightStructuredAsync(AIInsightRequest request)
        => Task.FromResult<AIInsightResponse?>(new AIInsightResponse());

    public Task<ChatbotResponse?> SendChatbotMessageAsync(ChatbotMessageRequest request)
        => Task.FromResult<ChatbotResponse?>(new ChatbotResponse { Success = true });

    public Task<ChatbotResponse?> SendChatbotQuickReplyAsync(ChatbotQuickReplyRequest request)
        => Task.FromResult<ChatbotResponse?>(new ChatbotResponse { Success = true });

    public Task<ChatbotResponse?> SendChatbotTriggerAsync(ChatbotTriggerRequest request)
        => Task.FromResult<ChatbotResponse?>(new ChatbotResponse { Success = true });

    public Task<bool> IsHealthyAsync() => Task.FromResult(Healthy);
}

/// <summary>Thay SendGrid: mọi email được ghi vào <see cref="Sent"/> ngay (đồng bộ).</summary>
public sealed class FakeEmailSink : IEmailService, IBackgroundEmailService
{
    public sealed record Mail(string Kind, string To, string Name, string? Link);

    private readonly List<Mail> _sent = new();
    public IReadOnlyList<Mail> Sent
    {
        get { lock (_sent) return _sent.ToList(); }
    }

    private void Add(Mail m) { lock (_sent) _sent.Add(m); }

    public string? LastLink(string kind)
        => Sent.LastOrDefault(m => m.Kind == kind)?.Link;

    public void Clear() { lock (_sent) _sent.Clear(); }

    // IEmailService
    public Task SendConfirmEmailAsync(string toEmail, string fullName, string confirmLink)
    { Add(new("confirm", toEmail, fullName, confirmLink)); return Task.CompletedTask; }

    public Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
    { Add(new("reset", toEmail, fullName, resetLink)); return Task.CompletedTask; }

    public Task SendTabSwitchNotificationAsync(string toEmail, string parentName, string studentName,
        string exerciseName, DateTime switchedAt, int switchCount)
    { Add(new("tab-switch", toEmail, parentName, null)); return Task.CompletedTask; }

    // IBackgroundEmailService
    public void QueueConfirmationEmail(string toEmail, string fullName, string confirmLink)
        => Add(new("confirm", toEmail, fullName, confirmLink));

    public void QueuePasswordResetEmail(string toEmail, string fullName, string resetLink)
        => Add(new("reset", toEmail, fullName, resetLink));

    public void QueueTabSwitchEmail(string toEmail, string parentName, string studentName,
        string exerciseName, DateTime switchedAt, int switchCount)
        => Add(new("tab-switch", toEmail, parentName, null));

    /// <summary>Trích token trong link (query <c>?token=</c> hoặc <c>&amp;token=</c>).</summary>
    public string? LastToken(string kind)
    {
        var link = LastLink(kind);
        if (link == null) return null;
        var m = Regex.Match(link, @"token=([^&\s]+)");
        return m.Success ? Uri.UnescapeDataString(m.Groups[1].Value) : null;
    }
}
