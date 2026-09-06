using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SweepResult = ELearning_ToanHocHay_Control.Services.Interfaces.LifecycleSweepResult;

namespace ELearning_ToanHocHay.Tests.Unit.Subscriptions;

/// <summary>§3.29 — UT-LIFE-*. SQLite (Subscriptions + Payments).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class SubscriptionLifecycleServiceTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly int _packageId;

    public SubscriptionLifecycleServiceTests()
        => _packageId = Seed.Package(_sql.Db).PackageId;

    public void Dispose() => _sql.Dispose();

    private static DateTime Now => DateTime.UtcNow;

    private Subscription AddSub(
        SubscriptionStatus status, DateTime endDate, DateTime? createdAt = null, Payment? payment = null)
    {
        var sub = Entities.NewSubscription(_packageId, status, endDate: endDate, tweak: s =>
        {
            s.CreatedAt = createdAt ?? Now;
            s.PaymentId = payment?.PaymentId;
        });
        _sql.Db.Subscriptions.Add(sub);
        _sql.Db.SaveChanges();
        return sub;
    }

    private SweepResult Sweep(int timeoutMinutes = 30)
    {
        var svc = new SubscriptionLifecycleService(
            _sql.NewContext(),
            Options.Create(new SePayOptions { PendingTimeoutMinutes = timeoutMinutes }),
            NullLogger<SubscriptionLifecycleService>.Instance);
        return svc.RunSweepAsync().GetAwaiter().GetResult();
    }

    private SubscriptionStatus StatusOf(int subId)
        => _sql.NewContext().Subscriptions.Single(s => s.SubscriptionId == subId).Status;

    [Fact] // UT-LIFE-01
    public void Active_past_end_date_expires()
    {
        var sub = AddSub(SubscriptionStatus.Active, endDate: Now.AddHours(-1));
        Sweep();
        StatusOf(sub.SubscriptionId).Should().Be(SubscriptionStatus.Expired);
    }

    [Fact] // UT-LIFE-02
    public void Active_within_end_date_stays_active()
    {
        var sub = AddSub(SubscriptionStatus.Active, endDate: Now.AddDays(10));
        Sweep();
        StatusOf(sub.SubscriptionId).Should().Be(SubscriptionStatus.Active);
    }

    [Fact] // UT-LIFE-03
    public void Stale_pending_is_cancelled_and_payment_failed()
    {
        var payment = Seed.Payment(_sql.Db, 199_000m, PaymentStatus.Pending);
        var sub = AddSub(SubscriptionStatus.Pending, endDate: Now.AddDays(30),
            createdAt: Now.AddMinutes(-45), payment: payment);

        Sweep(timeoutMinutes: 30);

        StatusOf(sub.SubscriptionId).Should().Be(SubscriptionStatus.Cancelled);
        _sql.NewContext().Payments.Single(p => p.PaymentId == payment.PaymentId)
            .Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact] // UT-LIFE-04
    public void Fresh_pending_is_left_alone()
    {
        var sub = AddSub(SubscriptionStatus.Pending, endDate: Now.AddDays(30), createdAt: Now.AddMinutes(-5));
        Sweep(timeoutMinutes: 30);
        StatusOf(sub.SubscriptionId).Should().Be(SubscriptionStatus.Pending);
    }

    [Fact] // UT-LIFE-05
    public void Nothing_to_do_returns_zero()
        => Sweep().Should().Be(new SweepResult(0, 0));

    [Fact] // UT-LIFE-06
    public void Counts_are_accurate_with_many_rows()
    {
        AddSub(SubscriptionStatus.Active, endDate: Now.AddHours(-1));
        AddSub(SubscriptionStatus.Active, endDate: Now.AddHours(-2));
        AddSub(SubscriptionStatus.Pending, endDate: Now.AddDays(30), createdAt: Now.AddHours(-2));

        Sweep().Should().Be(new SweepResult(2, 1));
    }

    [Fact] // UT-LIFE-07
    public void Already_terminal_subscriptions_are_untouched()
    {
        var cancelled = AddSub(SubscriptionStatus.Cancelled, endDate: Now.AddHours(-1));
        var expired = AddSub(SubscriptionStatus.Expired, endDate: Now.AddHours(-1));

        Sweep().Should().Be(new SweepResult(0, 0));
        StatusOf(cancelled.SubscriptionId).Should().Be(SubscriptionStatus.Cancelled);
        StatusOf(expired.SubscriptionId).Should().Be(SubscriptionStatus.Expired);
    }
}
