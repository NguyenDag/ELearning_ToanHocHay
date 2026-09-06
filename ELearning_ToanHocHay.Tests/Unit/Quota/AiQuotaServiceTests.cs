using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Quota;

/// <summary>§3.28 — UT-QUOTA-*. SQLite (AiUsageDaily) + package repo / config fake.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class AiQuotaServiceTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly IPackageRepository _packages = Substitute.For<IPackageRepository>();
    private readonly FakeSystemConfig _config = new();
    private readonly int _studentId;

    public AiQuotaServiceTests()
        => _studentId = Seed.Student(_sql.Db).Student.StudentId;

    public void Dispose() => _sql.Dispose();

    private AiQuotaService Svc(string freeLimitConfig = "3")
        => new(_sql.NewContext(), _packages, _config,
            TestConfig.Build(("AI:FreeDailyHintLimit", freeLimitConfig)));

    private void FreeTier() => _packages.GetActivePackageAsync(_studentId).Returns((Subscription?)null);

    private void PackageTier(bool unlimited, int? dailyLimit = null)
        => _packages.GetActivePackageAsync(_studentId).Returns(new Subscription
        {
            Package = new Package { PackageName = "p", UnlimitedAiHint = unlimited, AiHintLimitDaily = dailyLimit }
        });

    private void SetUsage(int hintCount, DateOnly? date = null)
    {
        _sql.Db.AiUsageDailies.Add(new AiUsageDaily
        {
            StudentId = _studentId,
            Date = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            HintCount = hintCount,
        });
        _sql.Db.SaveChanges();
    }

    private int StoredHint() => _sql.NewContext().AiUsageDailies
        .Where(u => u.StudentId == _studentId && u.Date == DateOnly.FromDateTime(DateTime.UtcNow))
        .Select(u => (int?)u.HintCount).FirstOrDefault() ?? 0;

    [Fact] // UT-QUOTA-01
    public async Task Free_no_usage_peek()
    {
        FreeTier();
        var q = await Svc().PeekHintAsync(_studentId);
        (q.Allowed, q.Used, q.Limit, q.Unlimited).Should().Be((true, 0, 3, false));
    }

    [Fact] // UT-QUOTA-02
    public async Task Free_under_limit_peek_allowed()
    {
        FreeTier();
        SetUsage(2);
        (await Svc().PeekHintAsync(_studentId)).Allowed.Should().BeTrue();
    }

    [Fact] // UT-QUOTA-03
    public async Task Free_at_limit_peek_denied()
    {
        FreeTier();
        SetUsage(3);
        (await Svc().PeekHintAsync(_studentId)).Allowed.Should().BeFalse();
    }

    [Fact] // UT-QUOTA-04
    public async Task Unlimited_package_always_allowed()
    {
        PackageTier(unlimited: true);
        SetUsage(999);
        (await Svc().PeekHintAsync(_studentId)).Allowed.Should().BeTrue();
    }

    [Fact] // UT-QUOTA-05
    public async Task Consume_under_limit_increments()
    {
        FreeTier();
        var q = await Svc().TryConsumeHintAsync(_studentId);
        q.Allowed.Should().BeTrue();
        StoredHint().Should().Be(1);
    }

    [Fact] // UT-QUOTA-06
    public async Task Consume_at_limit_is_denied_without_increment()
    {
        FreeTier();
        SetUsage(3);
        var q = await Svc().TryConsumeHintAsync(_studentId);
        q.Allowed.Should().BeFalse();
        StoredHint().Should().Be(3);
    }

    [Fact] // UT-QUOTA-07
    public async Task Consume_unlimited_still_counts()
    {
        PackageTier(unlimited: true);
        SetUsage(50);
        var q = await Svc().TryConsumeHintAsync(_studentId);
        q.Allowed.Should().BeTrue();
        StoredHint().Should().Be(51);
    }

    [Fact] // UT-QUOTA-08
    public async Task Yesterdays_usage_does_not_count_today()
    {
        FreeTier();
        SetUsage(3, date: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        (await Svc().PeekHintAsync(_studentId)).Used.Should().Be(0);
    }

    [Fact] // UT-QUOTA-09
    public async Task Free_limit_falls_back_to_app_config_when_system_config_missing()
    {
        FreeTier();
        (await Svc(freeLimitConfig: "5").PeekHintAsync(_studentId)).Limit.Should().Be(5);
    }

    [Fact] // UT-QUOTA-10
    public async Task Free_limit_falls_back_to_3_when_app_config_unparseable()
    {
        FreeTier();
        (await Svc(freeLimitConfig: "xyz").PeekHintAsync(_studentId)).Limit.Should().Be(3);
    }

    [Fact] // UT-QUOTA-11
    public async Task RecordFeedback_increments_feedback_count()
    {
        await Svc().RecordFeedbackAsync(_studentId);
        _sql.NewContext().AiUsageDailies.Single().FeedbackCount.Should().Be(1);
    }
}
