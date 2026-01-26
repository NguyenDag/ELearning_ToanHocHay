namespace ELearning_ToanHocHay_Control.Models.DTOs.Subscription
{
    public class CreateSubscriptionDto
    {
        public int StudentId { get; set; }
        public int PackageId { get; set; }
        public int PaymentId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal AmountPaid { get; set; }
    }
}
