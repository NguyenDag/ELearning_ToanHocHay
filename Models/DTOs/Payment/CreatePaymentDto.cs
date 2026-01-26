using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Payment
{
    public class CreatePaymentDto
    {
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
