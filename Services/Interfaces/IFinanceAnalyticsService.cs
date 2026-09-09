using ELearning_ToanHocHay_Control.Models.DTOs.Finance;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>Tổng hợp doanh thu / giao dịch cho khu quản trị tài chính (biểu đồ + KPI).</summary>
    public interface IFinanceAnalyticsService
    {
        Task<RevenueSummaryDto> GetSummaryAsync(DateTime from, DateTime to);
        Task<List<RevenueBucketDto>> GetRevenueTimeSeriesAsync(DateTime from, DateTime to, RevenueInterval interval);
        Task<List<PackageRevenueDto>> GetRevenueByPackageAsync(DateTime from, DateTime to);
    }
}
