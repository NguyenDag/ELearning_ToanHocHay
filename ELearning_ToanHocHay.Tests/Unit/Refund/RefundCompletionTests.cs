using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Refund;

/// <summary>§3.20 — UT-RFC-*. Hiệu ứng entity thuần.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class RefundCompletionTests
{
    private static Payment Pay(decimal amount, decimal? alreadyRefunded, SubscriptionStatus? subStatus)
        => Entities.NewPayment(payerUserId: 1, amount: amount, PaymentStatus.Completed, tweak: p =>
        {
            p.RefundAmount = alreadyRefunded;
            p.Subscription = subStatus is null ? null : Entities.NewSubscription(packageId: 1, status: subStatus.Value);
        });

    private static RefundRequest Req(decimal amount)
        => Entities.NewRefundRequest(paymentId: 1, amount: amount);

    [Fact] // UT-RFC-01
    public void Full_refund_active_subscription()
    {
        var payment = Pay(199_000m, null, SubscriptionStatus.Active);

        RefundCompletion.Apply(payment, Req(199_000m));

        payment.RefundAmount.Should().Be(199_000m);
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAt.Should().NotBeNull();
        payment.Subscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact] // UT-RFC-02
    public void Partial_refund_keeps_subscription_active()
    {
        var payment = Pay(199_000m, null, SubscriptionStatus.Active);

        RefundCompletion.Apply(payment, Req(100_000m));

        payment.RefundAmount.Should().Be(100_000m);
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.Subscription!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact] // UT-RFC-03
    public void Refund_accumulates_to_full()
    {
        var payment = Pay(199_000m, 100_000m, SubscriptionStatus.Active);

        RefundCompletion.Apply(payment, Req(99_000m));

        payment.RefundAmount.Should().Be(199_000m);
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact] // UT-RFC-04
    public void Full_refund_pending_subscription_is_cancelled()
    {
        var payment = Pay(199_000m, null, SubscriptionStatus.Pending);

        RefundCompletion.Apply(payment, Req(199_000m));

        payment.Subscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact] // UT-RFC-05
    public void Full_refund_expired_subscription_untouched()
    {
        var payment = Pay(199_000m, null, SubscriptionStatus.Expired);

        RefundCompletion.Apply(payment, Req(199_000m));

        payment.Subscription!.Status.Should().Be(SubscriptionStatus.Expired);
    }

    [Fact] // UT-RFC-06
    public void Full_refund_without_subscription_does_not_throw()
    {
        var payment = Pay(199_000m, null, subStatus: null);

        var act = () => RefundCompletion.Apply(payment, Req(199_000m));

        act.Should().NotThrow();
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact] // UT-RFC-07 — hàm không tự chặn quá số tiền
    public void Over_refund_is_not_blocked_here()
    {
        var payment = Pay(199_000m, null, SubscriptionStatus.Active);

        RefundCompletion.Apply(payment, Req(250_000m));

        payment.RefundAmount.Should().Be(250_000m);
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }
}
