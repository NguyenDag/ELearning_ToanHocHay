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

            var refunded = (payment.RefundAmount ?? 0m) + request.Amount;
            payment.RefundAmount = refunded;
            payment.RefundedAt = DateTime.UtcNow;

            var fullyRefunded = refunded >= payment.Amount;
            payment.Status = fullyRefunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

            if (fullyRefunded && payment.Subscription is
                { Status: SubscriptionStatus.Active or SubscriptionStatus.Pending })
                payment.Subscription.Status = SubscriptionStatus.Cancelled;
        }
    }
}
