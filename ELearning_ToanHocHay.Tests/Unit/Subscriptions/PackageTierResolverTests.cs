using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Subscriptions;

/// <summary>§3.30 — UT-TIER-*. SQLite (Subscriptions + Packages).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class PackageTierResolverTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero));

    private readonly int _studentId;

    public PackageTierResolverTests()
        => _studentId = Seed.Student(_sql.Db).Student.StudentId;

    public void Dispose() => _sql.Dispose();

    private void AddSubscription(PackageTier tier, SubscriptionStatus status, DateTime? endDate = null)
    {
        var package = Seed.Package(_sql.Db, tier);
        _sql.Db.Subscriptions.Add(Entities.NewSubscription(
            package.PackageId, status, studentId: _studentId,
            endDate: endDate ?? _clock.GetUtcNow().UtcDateTime.AddDays(30)));
        _sql.Db.SaveChanges();
    }

    private Task<PackageTier> Resolve()
        => new PackageTierResolver(_sql.NewContext(), _clock).ResolveAsync(_studentId);

    [Fact] // UT-TIER-01
    public async Task No_subscription_is_free()
        => (await Resolve()).Should().Be(PackageTier.Free);

    [Fact] // UT-TIER-02
    public async Task Single_active_subscription_returns_its_tier()
    {
        AddSubscription(PackageTier.Standard, SubscriptionStatus.Active);
        (await Resolve()).Should().Be(PackageTier.Standard);
    }

    [Fact] // UT-TIER-03
    public async Task Highest_tier_among_concurrent_active_wins()
    {
        AddSubscription(PackageTier.Standard, SubscriptionStatus.Active);
        AddSubscription(PackageTier.Premium, SubscriptionStatus.Active);
        (await Resolve()).Should().Be(PackageTier.Premium);
    }

    [Fact] // UT-TIER-04
    public async Task Active_but_past_end_date_is_free()
    {
        AddSubscription(PackageTier.Premium, SubscriptionStatus.Active,
            endDate: _clock.GetUtcNow().UtcDateTime.AddDays(-1));
        (await Resolve()).Should().Be(PackageTier.Free);
    }

    [Theory] // UT-TIER-05
    [InlineData(SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Pending)]
    public async Task Only_non_active_subscriptions_is_free(SubscriptionStatus status)
    {
        AddSubscription(PackageTier.Premium, status);
        (await Resolve()).Should().Be(PackageTier.Free);
    }

    [Fact] // UT-TIER-06
    public async Task Two_active_same_tier_still_returns_that_tier()
    {
        AddSubscription(PackageTier.Standard, SubscriptionStatus.Active,
            endDate: _clock.GetUtcNow().UtcDateTime.AddDays(10));
        AddSubscription(PackageTier.Standard, SubscriptionStatus.Active,
            endDate: _clock.GetUtcNow().UtcDateTime.AddDays(40));
        (await Resolve()).Should().Be(PackageTier.Standard);
    }
}
