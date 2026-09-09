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

        /// <summary>When the pending order was created (UTC). A reload reuses the same order,
        /// so the checkout page can keep counting down from here instead of restarting.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>When the QR stops being valid (UTC) = <see cref="CreatedAt"/> + QrTimeoutMinutes.</summary>
        public DateTime ExpiresAt { get; set; }
    }
}
