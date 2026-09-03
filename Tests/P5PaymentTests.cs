using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// P5 — payments &amp; subscription lifecycle: idempotent transactional IPN,
/// the lifecycle sweep, the one-Active guard, reconciliation, "my" endpoints.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class P5PaymentTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public P5PaymentTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    private HttpClient IpnClient(string key = "test-sepay-key")
    {
        var c = _f.CreateClient();
        c.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Apikey {key}");
        return c;
    }

    /// <summary>Creates a Pending subscription for student A via the API; returns (subscriptionId, amount).</summary>
    private async Task<(int subId, long amount)> CreatePendingAsync()
    {
        var res = await _f.ClientFor(Id.StudentAUserId)
            .PostAsJsonAsync("/api/subscriptions", new { StudentId = Id.StudentAId, PackageId = Id.PackageId });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        var root = await Root(res);
        return (root.GetProperty("subscriptionId").GetInt32(), (long)root.GetProperty("amount").GetDecimal());
    }

    private static object Ipn(int subId, long amount, string reference, string type = "in")
        => new
        {
            id = Random.Shared.Next(1, int.MaxValue),
            content = $"SUBSCRIPTION_{subId}",
            transferType = type,
            transferAmount = amount,
            referenceCode = reference
        };

    [SkippableFact]
    public async Task Valid_IPN_activates_the_subscription_with_package_duration()
    {
        RequireDocker();
        var (subId, amount) = await CreatePendingAsync();
        var reference = $"REF-{Guid.NewGuid():N}";

        var res = await IpnClient().PostAsJsonAsync("/api/sepay/ipn", Ipn(subId, amount, reference));
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        (await Root(res)).GetProperty("outcome").GetString().Should().Be("Processed");

        var sub = await _f.QueryDbAsync(db => db.Subscriptions.Include(s => s.Payment)
            .SingleAsync(s => s.SubscriptionId == subId));
        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.Payment!.Status.Should().Be(PaymentStatus.Completed);
        sub.Payment.TransactionId.Should().Be(reference);
        (sub.EndDate - sub.StartDate).TotalDays.Should().BeApproximately(30, 0.1); // package DurationDays

        var log = await _f.QueryDbAsync(db => db.SePayIpnLogs.AsNoTracking()
            .SingleAsync(l => l.ReferenceCode == reference));
        log.Outcome.Should().Be(IpnOutcome.Processed);
    }

    [SkippableFact]
    public async Task Replaying_the_same_referenceCode_is_idempotent()
    {
        RequireDocker();
        var (subId, amount) = await CreatePendingAsync();
        var reference = $"REF-{Guid.NewGuid():N}";

        var first = await IpnClient().PostAsJsonAsync("/api/sepay/ipn", Ipn(subId, amount, reference));
        (await Root(first)).GetProperty("outcome").GetString().Should().Be("Processed");

        var second = await IpnClient().PostAsJsonAsync("/api/sepay/ipn", Ipn(subId, amount, reference));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Root(second)).GetProperty("outcome").GetString().Should().Be("Duplicate");

        var logCount = await _f.QueryDbAsync(db => db.SePayIpnLogs.CountAsync(l => l.ReferenceCode == reference));
        logCount.Should().Be(1);
    }

    [SkippableFact]
    public async Task Wrong_amount_does_not_activate()
    {
        RequireDocker();
        var (subId, amount) = await CreatePendingAsync();

        var res = await IpnClient().PostAsJsonAsync("/api/sepay/ipn",
            Ipn(subId, amount - 50_000, $"REF-{Guid.NewGuid():N}"));
        (await Root(res)).GetProperty("outcome").GetString().Should().Be("AmountMismatch");

        var status = await _f.QueryDbAsync(db => db.Subscriptions.AsNoTracking()
            .Where(s => s.SubscriptionId == subId).Select(s => s.Status).SingleAsync());
        status.Should().Be(SubscriptionStatus.Pending);
    }

    [SkippableFact]
    public async Task Out_transfer_and_unknown_subscription_are_ignored_with_200()
    {
        RequireDocker();

        var outRes = await IpnClient().PostAsJsonAsync("/api/sepay/ipn",
            Ipn(1, 1000, $"REF-{Guid.NewGuid():N}", type: "out"));
        outRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Root(outRes)).GetProperty("outcome").GetString().Should().Be("Ignored");

        var unknownRes = await IpnClient().PostAsJsonAsync("/api/sepay/ipn",
            Ipn(999_999, 1000, $"REF-{Guid.NewGuid():N}"));
        unknownRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Root(unknownRes)).GetProperty("message").GetString().Should().Contain("not found");
    }

    [SkippableFact]
    public async Task IPN_rejects_a_bad_api_key()
    {
        RequireDocker();
        var res = await IpnClient("wrong-key").PostAsJsonAsync("/api/sepay/ipn",
            Ipn(1, 1000, $"REF-{Guid.NewGuid():N}"));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Activating_a_second_subscription_expires_the_first()
    {
        RequireDocker();
        var (sub1, amount) = await CreatePendingAsync();
        await IpnClient().PostAsJsonAsync("/api/sepay/ipn", Ipn(sub1, amount, $"REF-{Guid.NewGuid():N}"));

        var (sub2, amount2) = await CreatePendingAsync();
        await IpnClient().PostAsJsonAsync("/api/sepay/ipn", Ipn(sub2, amount2, $"REF-{Guid.NewGuid():N}"));

        var statuses = await _f.QueryDbAsync(db => db.Subscriptions.AsNoTracking()
            .Where(s => s.SubscriptionId == sub1 || s.SubscriptionId == sub2)
            .ToDictionaryAsync(s => s.SubscriptionId, s => s.Status));

        statuses[sub1].Should().Be(SubscriptionStatus.Expired);
        statuses[sub2].Should().Be(SubscriptionStatus.Active);
    }

    [SkippableFact]
    public async Task Lifecycle_sweep_expires_past_due_and_releases_stale_pending()
    {
        RequireDocker();

        var seeded = await _f.QueryDbAsync(async db =>
        {
            var pastDue = new Subscription
            {
                StudentId = Id.StudentBId, PackageId = Id.PackageId, Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-40), EndDate = DateTime.UtcNow.AddDays(-1),
                AmountPaid = 199000, CreatedAt = DateTime.UtcNow.AddDays(-40)
            };
            var stalePending = new Subscription
            {
                StudentId = Id.StudentBId, PackageId = Id.PackageId, Status = SubscriptionStatus.Pending,
                StartDate = DateTime.UtcNow.AddHours(-5), EndDate = DateTime.UtcNow.AddDays(30),
                AmountPaid = 199000, CreatedAt = DateTime.UtcNow.AddHours(-5)
            };
            db.Subscriptions.AddRange(pastDue, stalePending);
            await db.SaveChangesAsync();
            return new { pastDue = pastDue.SubscriptionId, stalePending = stalePending.SubscriptionId };
        });

        var res = await _f.ClientFor(Id.AdminUserId).PostAsync("/api/finance/subscriptions/run-lifecycle", null);
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());

        var statuses = await _f.QueryDbAsync(db => db.Subscriptions.AsNoTracking()
            .Where(s => s.SubscriptionId == seeded.pastDue || s.SubscriptionId == seeded.stalePending)
            .ToDictionaryAsync(s => s.SubscriptionId, s => s.Status));

        statuses[seeded.pastDue].Should().Be(SubscriptionStatus.Expired);
        statuses[seeded.stalePending].Should().Be(SubscriptionStatus.Cancelled);
    }

    [SkippableFact]
    public async Task Reconciliation_and_my_endpoints_are_role_gated()
    {
        RequireDocker();

        (await _f.ClientFor(Id.StudentAUserId).GetAsync("/api/finance/subscriptions/reconciliation"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var recon = await _f.ClientFor(Id.FinanceUserId).GetAsync("/api/finance/subscriptions/reconciliation");
        recon.StatusCode.Should().Be(HttpStatusCode.OK, await recon.Content.ReadAsStringAsync());
        (await Root(recon)).TryGetProperty("Balanced", out _).Should().BeTrue();

        (await _f.ClientFor(Id.StudentAUserId).GetAsync("/api/subscriptions/me"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _f.ClientFor(Id.StudentAUserId).GetAsync("/api/payments/me"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
