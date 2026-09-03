namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public record QuotaCheck(bool Allowed, int Used, int Limit, bool Unlimited)
    {
        public int Remaining => Unlimited ? int.MaxValue : Math.Max(0, Limit - Used);
    }

    /// <summary>P6 — daily AI usage limits driven by the student's package.</summary>
    public interface IAiQuotaService
    {
        Task<QuotaCheck> PeekHintAsync(int studentId);

        /// <summary>Consumes one hint if the student is under their daily limit.</summary>
        Task<QuotaCheck> TryConsumeHintAsync(int studentId);

        /// <summary>Records an auto-generated feedback (not gated — for cost visibility).</summary>
        Task RecordFeedbackAsync(int studentId);

        Task RecordChatAsync(int studentId);
    }
}
