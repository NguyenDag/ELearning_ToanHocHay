using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Áp hiệu ứng lên <see cref="Payment"/> + <see cref="Subscription"/> khi một
    /// <see cref="RefundRequest"/> chuyển sang Completed. Dùng chung bởi confirm đơn lẻ và confirm cả lô.
    /// Chỉ sửa entity trong change tracker — caller SaveChanges (trong transaction).
    /// </summary>
    public static class RefundCompletion
    {
        public static async Task ApplyAsync(AppDbContext ctx, RefundRequest request)
        {
            var payment = await ctx.Payments
                .Include(p => p.Subscription)
                .FirstAsync(p => p.PaymentId == request.PaymentId);

            Apply(payment, request);
        }

        /// <summary>
        /// Hiệu ứng thuần trên entity (<see cref="Payment"/> đã <c>Include</c> <see cref="Subscription"/>):
        /// cộng dồn <c>RefundAmount</c>, chuyển <c>Status</c> sang Refunded / PartiallyRefunded, và huỷ
        /// subscription khi hoàn toàn phần (chỉ khi subscription đang Active / Pending). Không chạm DB.
        /// </summary>
        public static void Apply(Payment paymentWithSubscription, RefundRequest request)
        {
            var refunded = (paymentWithSubscription.RefundAmount ?? 0m) + request.Amount;
            paymentWithSubscription.RefundAmount = refunded;
            paymentWithSubscription.RefundedAt = DateTime.UtcNow;

            var fullyRefunded = refunded >= paymentWithSubscription.Amount;
            paymentWithSubscription.Status = fullyRefunded
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;

            if (fullyRefunded && paymentWithSubscription.Subscription is
                { Status: SubscriptionStatus.Active or SubscriptionStatus.Pending })
                paymentWithSubscription.Subscription.Status = SubscriptionStatus.Cancelled;
        }
    }
}
