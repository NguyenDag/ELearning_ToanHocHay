using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Finance;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// P5 — finance operations: subscription lifecycle sweep + reconciliation.
    /// Pha 2 — semi-automatic refund workflow: review / approve / batch / export CSV / confirm.
    /// </summary>
    [Route("api/finance")]
    [ApiController]
    [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
    public class FinanceController : ControllerBase
    {
        private readonly ISubscriptionLifecycleService _lifecycle;
        private readonly IRefundService _refunds;
        private readonly IRefundBatchService _batches;
        private readonly IFinanceAnalyticsService _analytics;

        public FinanceController(
            ISubscriptionLifecycleService lifecycle,
            IRefundService refunds,
            IRefundBatchService batches,
            IFinanceAnalyticsService analytics)
        {
            _lifecycle = lifecycle;
            _refunds = refunds;
            _batches = batches;
            _analytics = analytics;
        }

        // ---------------------------------------------------------------- revenue analytics

        /// <summary>Khoảng ngày báo cáo: mặc định 30 ngày gần nhất, tối đa 366 ngày, <c>to</c> hết ngày.</summary>
        private static (DateTime From, DateTime To) ResolveRange(DateTime? from, DateTime? to)
        {
            var toDate = (to?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);
            var fromDate = from?.Date ?? toDate.Date.AddDays(-29);
            if (fromDate > toDate) (fromDate, toDate) = (toDate.Date, fromDate.Date.AddDays(1).AddTicks(-1));
            if ((toDate - fromDate).TotalDays > 366) fromDate = toDate.AddDays(-366);
            return (fromDate, toDate);
        }

        [HttpGet("analytics/summary")]
        public async Task<IActionResult> RevenueSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var (f, t) = ResolveRange(from, to);
            return Ok(ApiResponse<RevenueSummaryDto>.SuccessResponse(await _analytics.GetSummaryAsync(f, t)));
        }

        [HttpGet("analytics/revenue-series")]
        public async Task<IActionResult> RevenueSeries(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] RevenueInterval interval = RevenueInterval.Day)
        {
            var (f, t) = ResolveRange(from, to);
            return Ok(ApiResponse<List<RevenueBucketDto>>.SuccessResponse(
                await _analytics.GetRevenueTimeSeriesAsync(f, t, interval)));
        }

        [HttpGet("analytics/by-package")]
        public async Task<IActionResult> RevenueByPackage([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var (f, t) = ResolveRange(from, to);
            return Ok(ApiResponse<List<PackageRevenueDto>>.SuccessResponse(
                await _analytics.GetRevenueByPackageAsync(f, t)));
        }

        // ---------------------------------------------------------------- subscriptions

        [HttpPost("subscriptions/run-lifecycle")]
        public async Task<IActionResult> RunLifecycle()
        {
            var result = await _lifecycle.RunSweepAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new { result.ExpiredActive, result.ReleasedPending }));
        }

        [HttpGet("subscriptions/reconciliation")]
        public async Task<IActionResult> Reconciliation()
            => Ok(await _lifecycle.BuildReconciliationAsync());

        // ---------------------------------------------------------------- refund requests

        [HttpPost("refunds")]
        [EnableRateLimiting("refund")]
        public async Task<IActionResult> CreateRefund([FromBody] CreateRefundRequestDto dto)
            => (await _refunds.CreateAsync(dto, User)).ToActionResult();

        [HttpGet("refunds")]
        public async Task<IActionResult> ListRefunds([FromQuery] PagedRequest request,
            [FromQuery] RefundRequestStatus? status)
            => (await _refunds.ListAsync(request, status)).ToActionResult();

        [HttpGet("refunds/daily-usage")]
        public async Task<IActionResult> RefundDailyUsage()
            => (await _refunds.GetDailyUsageAsync()).ToActionResult();

        [HttpGet("refunds/reconciliation")]
        public async Task<IActionResult> RefundReconciliation()
            => Ok(await _refunds.BuildReconciliationAsync());

        [HttpGet("refunds/{id:int}")]
        public async Task<IActionResult> GetRefund(int id)
            => (await _refunds.GetByIdAsync(id, User)).ToActionResult();

        [HttpPost("refunds/{id:int}/approve")]
        public async Task<IActionResult> ApproveRefund(int id, [FromBody] ApproveRefundDto dto)
            => (await _refunds.ApproveAsync(id, dto, User)).ToActionResult();

        [HttpPost("refunds/{id:int}/reject")]
        public async Task<IActionResult> RejectRefund(int id, [FromBody] RejectRefundDto dto)
            => (await _refunds.RejectAsync(id, dto, User)).ToActionResult();

        [HttpPost("refunds/{id:int}/cancel")]
        public async Task<IActionResult> CancelRefund(int id, [FromBody] CancelRefundDto dto)
            => (await _refunds.CancelAsync(id, dto, User)).ToActionResult();

        [HttpPost("refunds/{id:int}/confirm")]
        public async Task<IActionResult> ConfirmRefund(int id, [FromBody] ConfirmRefundDto dto)
            => (await _refunds.ConfirmAsync(id, dto, User)).ToActionResult();

        [HttpPost("refunds/{id:int}/mark-failed")]
        public async Task<IActionResult> MarkRefundFailed(int id, [FromBody] MarkRefundFailedDto dto)
            => (await _refunds.MarkFailedAsync(id, dto, User)).ToActionResult();

        [HttpPost("refunds/{id:int}/retry")]
        public async Task<IActionResult> RetryRefund(int id)
            => (await _refunds.RetryAsync(id, User)).ToActionResult();

        // ---------------------------------------------------------------- refund batches

        [HttpPost("refund-batches")]
        public async Task<IActionResult> CreateBatch([FromBody] CreateRefundBatchDto dto)
            => (await _batches.CreateAsync(dto, User)).ToActionResult();

        [HttpGet("refund-batches")]
        public async Task<IActionResult> ListBatches([FromQuery] PagedRequest request,
            [FromQuery] RefundBatchStatus? status)
            => (await _batches.ListAsync(request, status)).ToActionResult();

        [HttpGet("refund-batches/{id:int}")]
        public async Task<IActionResult> GetBatch(int id)
            => (await _batches.GetByIdAsync(id)).ToActionResult();

        [HttpGet("refund-batches/{id:int}/export")]
        public async Task<IActionResult> ExportBatch(int id)
        {
            var result = await _batches.ExportCsvAsync(id, User);
            if (!result.Success || result.Data == null)
                return result.ToActionResult();
            return File(result.Data.Content, "text/csv", result.Data.FileName);
        }

        [HttpPost("refund-batches/{id:int}/mark-disbursed")]
        public async Task<IActionResult> MarkBatchDisbursed(int id, [FromBody] MarkBatchDisbursedDto dto)
            => (await _batches.MarkDisbursedAsync(id, dto, User)).ToActionResult();

        [HttpPost("refund-batches/{id:int}/confirm-all")]
        public async Task<IActionResult> ConfirmBatch(int id)
            => (await _batches.ConfirmAllAsync(id, User)).ToActionResult();

        [HttpPost("refund-batches/{id:int}/cancel")]
        public async Task<IActionResult> CancelBatch(int id)
            => (await _batches.CancelAsync(id, User)).ToActionResult();
    }
}
