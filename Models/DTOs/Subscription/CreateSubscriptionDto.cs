namespace ELearning_ToanHocHay_Control.Models.DTOs.Subscription
{
    public class CreateSubscriptionDto
    {
        public int StudentId { get; set; }
        public int PackageId { get; set; }
    }

    /// <summary>Result of creating a pending subscription — the price is decided server-side.</summary>
    public class CreatePendingResultDto
    {
        public int SubscriptionId { get; set; }
        public decimal Amount { get; set; }
    }
}
