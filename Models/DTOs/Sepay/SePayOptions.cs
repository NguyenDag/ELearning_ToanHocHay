namespace ELearning_ToanHocHay_Control.Models.DTOs.Sepay
{
    public class SePayOptions
    {
        public string Env { get; set; }
        public string MerchantId { get; set; }
        public string SecretKey { get; set; }
        public string BaseUrl { get; set; }
        public string BankName { get; set; }
        public string VA { get; set; }
        public string ApiKeyValidator { get; set; }

        // P5 — lifecycle knobs
        public long AmountToleranceVnd { get; set; } = 0;      // accepted over/under-payment
        public int PendingTimeoutMinutes { get; set; } = 30;   // release stale Pending subscriptions
        public int LifecycleIntervalMinutes { get; set; } = 5; // background sweep cadence (<=0 disables)
    }
}
