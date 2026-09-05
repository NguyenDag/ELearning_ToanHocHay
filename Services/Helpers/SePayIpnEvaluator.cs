using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Ma trận quyết định cho một IPN "tiền vào": nhận request + subscription đã nạp
    /// (có thể null) + dung sai, trả <see cref="IpnOutcome"/>. Không chạm DB — phần ghi
    /// (kích hoạt subscription, cập nhật payment) do <see cref="Implementations.SePayIpnService"/> làm khi kết quả là
    /// <see cref="IpnOutcome.Processed"/>.
    /// </summary>
    public static class SePayIpnEvaluator
    {
        public static IpnOutcome Evaluate(SePayIpnRequest request, Subscription? subscription, decimal toleranceVnd)
        {
            if (!string.Equals(request.transferType, "in", StringComparison.OrdinalIgnoreCase))
                return IpnOutcome.Ignored;

            if (SePayContentParser.TryParseSubscriptionId(request.content) is null)
                return IpnOutcome.Ignored;

            if (subscription is null)
                return IpnOutcome.Ignored;

            if (subscription.Status == SubscriptionStatus.Active)
                return IpnOutcome.Duplicate;

            if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
                return IpnOutcome.Ignored;

            // Còn lại: Pending — kiểm tra số tiền.
            if (!SePayAmountMatcher.Matches(subscription.AmountPaid, request.transferAmount, toleranceVnd))
                return IpnOutcome.AmountMismatch;

            return IpnOutcome.Processed;
        }
    }
}
