using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Sepay;

/// <summary>§3.16 — UT-SEPAY-EVAL-*. Nhận entity đã nạp, không DB.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class SePayIpnEvaluatorTests
{
    private static SePayIpnRequest Req(string type = "in", string? content = "TKPTTS SUBSCRIPTION_5", long amount = 199000)
        => new() { transferType = type, content = content, transferAmount = amount };

    private static Subscription Sub(SubscriptionStatus status, decimal amountPaid = 199000m)
        => Entities.NewSubscription(packageId: 1, status: status, tweak: s => s.AmountPaid = amountPaid);

    [Fact] // UT-SEPAY-EVAL-01
    public void Out_transaction_is_ignored()
        => SePayIpnEvaluator.Evaluate(Req(type: "out"), Sub(SubscriptionStatus.Pending), 0)
            .Should().Be(IpnOutcome.Ignored);

    [Fact] // UT-SEPAY-EVAL-02
    public void In_without_subscription_marker_is_ignored()
        => SePayIpnEvaluator.Evaluate(Req(content: "chuyen tien"), Sub(SubscriptionStatus.Pending), 0)
            .Should().Be(IpnOutcome.Ignored);

    [Fact] // UT-SEPAY-EVAL-03
    public void Valid_content_but_no_subscription_is_ignored()
        => SePayIpnEvaluator.Evaluate(Req(), subscription: null, 0)
            .Should().Be(IpnOutcome.Ignored);

    [Fact] // UT-SEPAY-EVAL-04
    public void Active_subscription_is_duplicate()
        => SePayIpnEvaluator.Evaluate(Req(), Sub(SubscriptionStatus.Active), 0)
            .Should().Be(IpnOutcome.Duplicate);

    [Theory] // UT-SEPAY-EVAL-05 / 06
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Non_payable_subscription_is_ignored(SubscriptionStatus status)
        => SePayIpnEvaluator.Evaluate(Req(), Sub(status), 0).Should().Be(IpnOutcome.Ignored);

    [Fact] // UT-SEPAY-EVAL-07
    public void Pending_with_amount_mismatch()
        => SePayIpnEvaluator.Evaluate(Req(amount: 150000), Sub(SubscriptionStatus.Pending, amountPaid: 199000m), toleranceVnd: 0)
            .Should().Be(IpnOutcome.AmountMismatch);

    [Fact] // UT-SEPAY-EVAL-08
    public void Pending_with_matching_amount_is_processed()
        => SePayIpnEvaluator.Evaluate(Req(amount: 199000), Sub(SubscriptionStatus.Pending, amountPaid: 199000m), toleranceVnd: 0)
            .Should().Be(IpnOutcome.Processed);

    [Fact] // UT-SEPAY-EVAL-09
    public void Uppercase_IN_is_treated_as_in()
        => SePayIpnEvaluator.Evaluate(Req(type: "IN"), Sub(SubscriptionStatus.Pending, amountPaid: 199000m), toleranceVnd: 0)
            .Should().Be(IpnOutcome.Processed);
}
