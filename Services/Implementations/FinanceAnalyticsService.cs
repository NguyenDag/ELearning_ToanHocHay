using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Finance;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class FinanceAnalyticsService : IFinanceAnalyticsService
    {
        private readonly AppDbContext _context;

        public FinanceAnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueSummaryDto> GetSummaryAsync(DateTime from, DateTime to)
        {
            // Mọi giao dịch trong kỳ (theo ngày tạo) — dùng cho phân rã trạng thái / phương thức.
            var inRange = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
                .Select(p => new { p.Amount, p.Status, p.PaymentMethod, p.PaidByUserId })
                .ToListAsync();

            var completed = inRange.Where(p => p.Status == PaymentStatus.Completed).ToList();
            var gross = completed.Sum(p => p.Amount);

            var refunded = await _context.Payments
                .AsNoTracking()
                .Where(p => p.RefundedAt != null && p.RefundedAt >= from && p.RefundedAt <= to)
                .Select(p => p.RefundAmount ?? 0m)
                .ToListAsync();
            var refundedTotal = refunded.Sum();

            var payingCustomers = completed.Select(p => p.PaidByUserId).Distinct().Count();
            var net = gross - refundedTotal;

            var newSubs = await _context.Subscriptions
                .AsNoTracking()
                .CountAsync(s => s.StartDate >= from && s.StartDate <= to
                                 && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Expired));

            return new RevenueSummaryDto
            {
                From = from,
                To = to,
                GrossRevenue = gross,
                RefundedAmount = refundedTotal,
                NetRevenue = net,
                CompletedCount = completed.Count,
                PendingCount = inRange.Count(p => p.Status == PaymentStatus.Pending),
                FailedCount = inRange.Count(p => p.Status == PaymentStatus.Failed),
                PayingCustomers = payingCustomers,
                NewSubscriptions = newSubs,
                Arpu = payingCustomers > 0 ? Math.Round(net / payingCustomers, 2) : 0m,
                StatusBreakdown = inRange
                    .GroupBy(p => p.Status)
                    .Select(g => new StatusSliceDto { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
                    .OrderBy(s => s.Status)
                    .ToList(),
                MethodBreakdown = completed
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new MethodSliceDto { Method = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
                    .OrderByDescending(m => m.Amount)
                    .ToList()
            };
        }

        public async Task<List<RevenueBucketDto>> GetRevenueTimeSeriesAsync(
            DateTime from, DateTime to, RevenueInterval interval)
        {
            var completed = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate >= from && p.PaymentDate <= to)
                .Select(p => new { p.PaymentDate, p.Amount })
                .ToListAsync();

            var refunds = await _context.Payments
                .AsNoTracking()
                .Where(p => p.RefundedAt != null && p.RefundedAt >= from && p.RefundedAt <= to)
                .Select(p => new { RefundedAt = p.RefundedAt!.Value, Amount = p.RefundAmount ?? 0m })
                .ToListAsync();

            var gross = completed
                .GroupBy(p => BucketStart(p.PaymentDate, interval))
                .ToDictionary(g => g.Key, g => (Total: g.Sum(x => x.Amount), Count: g.Count()));

            var refundByBucket = refunds
                .GroupBy(r => BucketStart(r.RefundedAt, interval))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var result = new List<RevenueBucketDto>();
            foreach (var periodStart in EnumerateBuckets(from, to, interval))
            {
                gross.TryGetValue(periodStart, out var g);
                refundByBucket.TryGetValue(periodStart, out var refund);
                result.Add(new RevenueBucketDto
                {
                    PeriodStart = periodStart,
                    GrossRevenue = g.Total,
                    NetRevenue = g.Total - refund,
                    TransactionCount = g.Count
                });
            }

            return result;
        }

        public async Task<List<PackageRevenueDto>> GetRevenueByPackageAsync(DateTime from, DateTime to)
        {
            var rows = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed
                            && p.PaymentDate >= from && p.PaymentDate <= to
                            && p.Subscription != null && p.Subscription.Package != null)
                .Select(p => new
                {
                    p.Amount,
                    p.Subscription!.PackageId,
                    PackageName = p.Subscription.Package!.PackageName,
                    Tier = p.Subscription.Package.Tier
                })
                .ToListAsync();

            return rows
                .GroupBy(r => new { r.PackageId, r.PackageName, r.Tier })
                .Select(g => new PackageRevenueDto
                {
                    PackageId = g.Key.PackageId,
                    PackageName = g.Key.PackageName,
                    Tier = g.Key.Tier,
                    TransactionCount = g.Count(),
                    GrossRevenue = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.GrossRevenue)
                .ToList();
        }

        // ------------------------------------------------------------------ bucketing

        private static DateTime BucketStart(DateTime value, RevenueInterval interval)
        {
            var d = value.Date;
            return interval switch
            {
                RevenueInterval.Week => StartOfWeek(d),
                RevenueInterval.Month => new DateTime(d.Year, d.Month, 1),
                _ => d
            };
        }

        private static DateTime StartOfWeek(DateTime d)
        {
            // Tuần bắt đầu Thứ Hai.
            int diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return d.AddDays(-diff);
        }

        private static IEnumerable<DateTime> EnumerateBuckets(DateTime from, DateTime to, RevenueInterval interval)
        {
            var cursor = BucketStart(from, interval);
            var end = BucketStart(to, interval);
            while (cursor <= end)
            {
                yield return cursor;
                cursor = interval switch
                {
                    RevenueInterval.Week => cursor.AddDays(7),
                    RevenueInterval.Month => cursor.AddMonths(1),
                    _ => cursor.AddDays(1)
                };
            }
        }
    }
}
