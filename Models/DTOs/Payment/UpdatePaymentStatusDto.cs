using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Payment
{
    public class UpdatePaymentStatusDto
    {
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
    }

    public class RefundPaymentDto
    {
        /// <summary>Amount to refund; omit for a full refund.</summary>
        public decimal? Amount { get; set; }
        public string? Reason { get; set; }
    }
}
