using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Finance
{
    /// <summary>Bậc thời gian gộp doanh thu.</summary>
    public enum RevenueInterval
    {
        Day,
        Week,
        Month
    }

    public class RevenueSummaryDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        /// <summary>Tổng tiền các giao dịch đã hoàn tất trong kỳ.</summary>
        public decimal GrossRevenue { get; set; }

        /// <summary>Tổng tiền đã hoàn trả (RefundedAt trong kỳ).</summary>
        public decimal RefundedAmount { get; set; }

        /// <summary>GrossRevenue − RefundedAmount.</summary>
        public decimal NetRevenue { get; set; }

        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }

        /// <summary>Số người trả tiền khác nhau (có giao dịch hoàn tất trong kỳ).</summary>
        public int PayingCustomers { get; set; }

        /// <summary>Số thuê bao được kích hoạt trong kỳ (StartDate trong kỳ).</summary>
        public int NewSubscriptions { get; set; }

        /// <summary>Doanh thu ròng bình quân mỗi khách trả phí.</summary>
        public decimal Arpu { get; set; }

        public List<StatusSliceDto> StatusBreakdown { get; set; } = new();
        public List<MethodSliceDto> MethodBreakdown { get; set; } = new();
    }

    public class StatusSliceDto
    {
        public PaymentStatus Status { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class MethodSliceDto
    {
        public PaymentMethod Method { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class RevenueBucketDto
    {
        public DateTime PeriodStart { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public int TransactionCount { get; set; }
    }

    public class PackageRevenueDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = "";
        public PackageTier Tier { get; set; }
        public int TransactionCount { get; set; }
        public decimal GrossRevenue { get; set; }
    }
}
