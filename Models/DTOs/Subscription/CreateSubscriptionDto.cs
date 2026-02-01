namespace ELearning_ToanHocHay_Control.Models.DTOs.Subscription
{
    public class CreateSubscriptionDto
    {
        public int StudentId { get; set; }
        public int PackageId { get; set; }

        public decimal AmountPaid { get; set; }
    }
}
