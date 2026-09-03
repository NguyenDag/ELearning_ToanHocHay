namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public record LifecycleSweepResult(int ExpiredActive, int ReleasedPending);

    public record ReconciliationReport(
        int CompletedPaymentCount,
        decimal CompletedPaymentTotal,
        int ActiveSubscriptionCount,
        decimal ActiveSubscriptionAmountTotal,
        int ActiveWithoutCompletedPayment,
        int CompletedPaymentWithoutActiveSubscription,
        bool Balanced);

    /// <summary>P5 — subscription lifecycle sweep + finance reconciliation.</summary>
    public interface ISubscriptionLifecycleService
    {
        /// <summary>Expire past-due Active subscriptions and release stale Pending ones. Idempotent.</summary>
        Task<LifecycleSweepResult> RunSweepAsync();

        Task<ReconciliationReport> BuildReconciliationAsync();
    }
}
