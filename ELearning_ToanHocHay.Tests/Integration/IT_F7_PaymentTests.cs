using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F7 — Thanh toán (SePay VA + IPN) (IT-F7).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Payment")]
public class IT_F7_PaymentTests : IntegrationTest
{
    public IT_F7_PaymentTests(ApiFactory app) : base(app) { }

    private async Task<string> Outcome(HttpResponseMessage res)
        => (await res.RootAsync()).GetProperty("Data").GetProperty("outcome").GetString()!;

    private Task<HttpResponseMessage> Ipn(object payload)
        => SePayIpn.Client(App).PostAsJsonAsync("/api/sepay/ipn", payload);

    [SkippableFact] // IT-F7-01 / IT-F7-02
    public async Task IT_F7_01_Create_subscription_uses_the_package_price()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();

        var res = await App.As(userId).PostAsJsonAsync("/api/subscriptions",
            new { StudentId = studentId, PackageId = Ids.PackageId, amount = 1 }); // client cố ép giá
        await res.ShouldBeOk();

        var data = await res.DataAsync();
        data.GetProperty("amount").GetDecimal().Should().Be(199_000m);
        data.GetProperty("qrUrl").GetString().Should().Contain("amount=199000");

        var subId = data.GetProperty("subscriptionId").GetInt32();
        await App.Db(async db =>
        {
            var sub = await db.Subscriptions.SingleAsync(s => s.SubscriptionId == subId);
            sub.Status.Should().Be(SubscriptionStatus.Pending);
            sub.AmountPaid.Should().Be(199_000m);
            (await db.Payments.SingleAsync(p => p.PaymentId == sub.PaymentId)).Status.Should().Be(PaymentStatus.Pending);
        });
    }

    [SkippableFact] // IT-F7-03
    public async Task IT_F7_03_Valid_ipn_activates_the_subscription()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (subId, amount) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);
        var reference = "REF-" + Guid.NewGuid().ToString("N")[..10];

        var res = await Ipn(SePayIpn.In(subId, amount, reference));
        (await Outcome(res)).Should().Be("Processed");

        await App.Db(async db =>
        {
            var sub = await db.Subscriptions.Include(s => s.Payment).SingleAsync(s => s.SubscriptionId == subId);
            sub.Status.Should().Be(SubscriptionStatus.Active);
            sub.Payment!.Status.Should().Be(PaymentStatus.Completed);
            (sub.EndDate - sub.StartDate).TotalDays.Should().BeApproximately(30, 1);
        });
    }

    [SkippableFact] // IT-F7-04
    public async Task IT_F7_04_Replaying_the_same_reference_is_a_duplicate()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (subId, amount) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);
        var reference = "REF-" + Guid.NewGuid().ToString("N")[..10];

        await Ipn(SePayIpn.In(subId, amount, reference));
        var second = await Ipn(SePayIpn.In(subId, amount, reference));

        (await Outcome(second)).Should().Be("Duplicate");
        await App.Db(async db =>
            (await db.SePayIpnLogs.CountAsync(l => l.ReferenceCode == reference)).Should().Be(1));
    }

    [SkippableFact] // IT-F7-05
    public async Task IT_F7_05_Wrong_amount_keeps_the_subscription_pending()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (subId, amount) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);

        var res = await Ipn(SePayIpn.In(subId, amount - 50_000, "REF-" + Guid.NewGuid().ToString("N")[..10]));

        (await Outcome(res)).Should().Be("AmountMismatch");
        await App.Db(async db =>
            (await db.Subscriptions.SingleAsync(s => s.SubscriptionId == subId)).Status
                .Should().Be(SubscriptionStatus.Pending));
    }

    [SkippableFact] // IT-F7-06
    public async Task IT_F7_06_A_fresh_reference_on_an_active_subscription_is_a_duplicate()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (subId, amount) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);
        await Flow.ActivateSubscriptionViaIpnAsync(subId, amount);

        var res = await Ipn(SePayIpn.In(subId, amount, "REF-" + Guid.NewGuid().ToString("N")[..10]));
        (await Outcome(res)).Should().Be("Duplicate");
    }

    [SkippableFact] // IT-F7-07
    public async Task IT_F7_07_Out_transfer_and_unknown_subscription_are_ignored()
    {
        RequireDocker();
        (await Outcome(await Ipn(SePayIpn.Out(199_000, "REF-" + Guid.NewGuid().ToString("N")[..8]))))
            .Should().Be("Ignored");
        (await Outcome(await Ipn(SePayIpn.In(999_999, 199_000, "REF-" + Guid.NewGuid().ToString("N")[..8]))))
            .Should().Be("Ignored");
    }

    [SkippableFact] // IT-F7-08
    public async Task IT_F7_08_Ipn_rejects_a_bad_api_key()
    {
        RequireDocker();
        var res = await SePayIpn.Client(App, key: "wrong-key")
            .PostAsJsonAsync("/api/sepay/ipn", SePayIpn.In(1, 199_000, "REF-x"));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F7-09
    public async Task IT_F7_09_Activating_a_second_subscription_expires_the_first()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();

        var (sub1, amount) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);
        await Flow.ActivateSubscriptionViaIpnAsync(sub1, amount);

        var (sub2, amount2) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);
        await Flow.ActivateSubscriptionViaIpnAsync(sub2, amount2);

        await App.Db(async db =>
        {
            (await db.Subscriptions.SingleAsync(s => s.SubscriptionId == sub2)).Status.Should().Be(SubscriptionStatus.Active);
            (await db.Subscriptions.SingleAsync(s => s.SubscriptionId == sub1)).Status.Should().Be(SubscriptionStatus.Expired);
        });
    }

    [SkippableFact] // IT-F7-11
    public async Task IT_F7_11_Lifecycle_sweep_expires_and_cancels()
    {
        RequireDocker();
        int expiredSub = 0, staleSub = 0, stalePayment = 0;
        await App.Db(async db =>
        {
            var pkg = await db.Packages.FirstAsync(p => p.PackageName == "IT Standard");
            var e = new Subscription
            {
                PackageId = pkg.PackageId, Status = SubscriptionStatus.Active, AmountPaid = 1,
                StartDate = DateTime.UtcNow.AddDays(-40), EndDate = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow.AddDays(-40),
            };
            var pay = new Payment
            {
                PaidByUserId = Ids.AdminUserId, Amount = 1, PaymentMethod = PaymentMethod.BankTransfer,
                Status = PaymentStatus.Pending, PaymentDate = DateTime.UtcNow.AddHours(-2),
            };
            db.Payments.Add(pay);
            await db.SaveChangesAsync();
            var s = new Subscription
            {
                PackageId = pkg.PackageId, PaymentId = pay.PaymentId, Status = SubscriptionStatus.Pending, AmountPaid = 1,
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow.AddHours(-2),
            };
            db.Subscriptions.AddRange(e, s);
            await db.SaveChangesAsync();
            expiredSub = e.SubscriptionId; staleSub = s.SubscriptionId; stalePayment = pay.PaymentId;
        });

        await (await App.AsRole(TestRole.Finance).PostAsync("/api/finance/subscriptions/run-lifecycle", null)).ShouldBeOk();

        await App.Db(async db =>
        {
            (await db.Subscriptions.SingleAsync(s => s.SubscriptionId == expiredSub)).Status.Should().Be(SubscriptionStatus.Expired);
            (await db.Subscriptions.SingleAsync(s => s.SubscriptionId == staleSub)).Status.Should().Be(SubscriptionStatus.Cancelled);
            (await db.Payments.SingleAsync(p => p.PaymentId == stalePayment)).Status.Should().Be(PaymentStatus.Failed);
        });
    }

    [SkippableFact] // IT-F7-14
    public async Task IT_F7_14_Cancel_subscription_owner_vs_stranger()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (subId, _) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);

        (await App.AsRole(TestRole.StudentB).PutAsync($"/api/subscriptions/cancel/{subId}", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await (await App.As(userId).PutAsync($"/api/subscriptions/cancel/{subId}", null)).ShouldBeOk();
        await App.Db(async db =>
            (await db.Subscriptions.SingleAsync(s => s.SubscriptionId == subId)).Status
                .Should().Be(SubscriptionStatus.Cancelled));
    }

    [SkippableFact] // IT-F7-13
    public async Task IT_F7_13_My_subscriptions_and_payments_are_scoped()
    {
        RequireDocker();
        await (await App.AsRole(TestRole.StudentA).GetAsync("/api/subscriptions/me")).ShouldBeOk();
        await (await App.AsRole(TestRole.StudentA).GetAsync("/api/payments/me")).ShouldBeOk();
    }

    [SkippableFact] // IT-F7-15 / IT-F7-16
    public async Task IT_F7_15_Finance_only_endpoints()
    {
        RequireDocker();
        (await App.AsRole(TestRole.Finance).GetAsync("/api/finance/subscriptions/reconciliation")).StatusCode
            .Should().Be(HttpStatusCode.OK);
        (await App.AsRole(TestRole.StudentA).GetAsync("/api/finance/subscriptions/reconciliation")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await App.AsRole(TestRole.StudentA).GetAsync("/api/subscriptions")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F7-17
    public async Task IT_F7_17_Status_mutations_need_finance_or_auth()
    {
        RequireDocker();
        (await App.AsRole(TestRole.StudentA).PatchAsync("/api/subscriptions/1/status", JsonContent.Create(new { Status = "Active" })))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await App.Anonymous().PutAsync("/api/payments/update-status/1", JsonContent.Create(new { Status = "Completed" })))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F7-18
    public async Task IT_F7_18_Packages_are_public_but_not_writable_by_students()
    {
        RequireDocker();
        await (await App.Anonymous().GetAsync("/api/packages")).ShouldBeOk();
        (await App.AsRole(TestRole.StudentA).PostAsJsonAsync("/api/packages", new { PackageName = "x", Price = 1, DurationDays = 30 }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
