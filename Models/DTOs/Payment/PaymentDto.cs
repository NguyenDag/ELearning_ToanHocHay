using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Payment
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }

        public int StudentId { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? TransactionId { get; set; }
    }
}
