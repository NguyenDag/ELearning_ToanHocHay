using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// B0 — kiểm tra hạ tầng test tự nó chạy được: SQLite dựng schema, object mother round-trip,
/// fake/claims/data-protection hoạt động. Không phải case nghiệp vụ.
/// </summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class InfrastructureSmokeTests
{
    [Fact]
    public void SqliteDb_New_creates_schema_and_seed_rows()
    {
        using var sql = SqliteDb.New();

        sql.Db.GradeLevels.Count().Should().BeGreaterThan(0);   // SeedCatalog
        sql.Db.Subjects.Any(s => s.Code == "MATH").Should().BeTrue();
        sql.Db.SystemConfigs.Any(c => c.ConfigKey == "refund.dailyCapVnd").Should().BeTrue();
    }

    [Fact]
    public void Entities_and_context_round_trip_through_sqlite()
    {
        using var sql = SqliteDb.New();

        var user = Entities.NewUser(type: UserType.Student);
        sql.Db.Users.Add(user);
        sql.Db.SaveChanges();

        var student = Entities.NewStudent(user.UserId);
        sql.Db.Students.Add(student);
        sql.Db.SaveChanges();

        using var read = sql.NewContext();
        read.Students.Include(s => s.User).Single()
            .User!.Email.Should().Be(user.Email);
    }

    [Fact]
    public void FakeRefundFieldProtector_round_trips_and_flags_bad_input()
    {
        var p = new FakeRefundFieldProtector();
        p.Unprotect(p.Protect("0071000123456")).Should().Be("0071000123456");
        p.Last4("12-34 56 78").Should().Be("5678");

        p.ThrowOnUnprotect = true;
        Action bad = () => p.Unprotect(p.Protect("x"));
        bad.Should().Throw<Exception>();
    }

    [Fact]
    public async Task FakeSystemConfig_reads_dict_then_fallback()
    {
        var cfg = new FakeSystemConfig().Set("a.b", "42");
        (await cfg.GetIntAsync("a.b", 0)).Should().Be(42);
        (await cfg.GetIntAsync("missing", 7)).Should().Be(7);
    }

    [Fact]
    public void Claims_For_carries_custom_claims()
    {
        var principal = Claims.For(UserType.SystemAdmin, userId: 42, studentId: 5);
        principal.FindFirst(ELearning_ToanHocHay_Control.Common.CustomJwtClaims.UserId)!.Value.Should().Be("42");
        principal.FindFirst(ELearning_ToanHocHay_Control.Common.CustomJwtClaims.StudentId)!.Value.Should().Be("5");
    }

    [Fact]
    public void DataProtection_Ephemeral_round_trips_via_RefundFieldProtector()
    {
        var protector = new RefundFieldProtector(DataProtection.Ephemeral());
        var cipher = protector.Protect("0071000123456");
        cipher.Should().NotBe("0071000123456");
        protector.Unprotect(cipher).Should().Be("0071000123456");
    }

    [Fact]
    public void TestConfig_Jwt_preset_has_valid_secret()
    {
        TestConfig.Jwt()["JwtSettings:SecretKey"]!.Length.Should().BeGreaterThanOrEqualTo(32);
    }

    [Fact]
    public void FakeClock_advances()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        clock.Advance(TimeSpan.FromDays(1));
        clock.GetUtcNow().Should().Be(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
    }
}
