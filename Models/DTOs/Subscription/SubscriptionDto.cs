using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Subscription
{
    public class SubscriptionDto
    {
        public int SubscriptionId { get; set; }

        public int StudentId { get; set; }
        public int PackageId { get; set; }
        public int PaymentId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public SubscriptionStatus Status { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
