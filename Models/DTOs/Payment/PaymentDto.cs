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

        /// <summary>Tên học sinh thụ hưởng (tra cứu ở danh sách giao dịch).</summary>
        public string? StudentName { get; set; }

        /// <summary>Tên người trả (có thể là phụ huynh).</summary>
        public string? PayerName { get; set; }

        /// <summary>Tên gói gắn với giao dịch (qua thuê bao), nếu có.</summary>
        public string? PackageName { get; set; }

        public PackageTier? PackageTier { get; set; }
    }
}
