using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace ELearning_ToanHocHay.Tests.Unit.Common;

/// <summary>§3.33 — UT-CFG-*. SQLite (SystemConfig) + MemoryCache thật.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class SystemConfigServiceTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public void Dispose() { _sql.Dispose(); _cache.Dispose(); }

    private void AddConfig(string key, string? value, ConfigValueType type = ConfigValueType.String)
    {
        _sql.Db.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = key,
            ConfigValue = value,
            ConfigType = type,
            ConfigGroup = "test",
            UpdatedAt = DateTime.UtcNow,
        });
        _sql.Db.SaveChanges();
    }

    private ISystemConfigService Svc() => new SystemConfigService(_sql.NewContext(), _cache);

    [Fact] // UT-CFG-01
    public async Task GetInt_reads_stored_value()
    {
        AddConfig("t.int", "42", ConfigValueType.Int);
        (await Svc().GetIntAsync("t.int", 0)).Should().Be(42);
    }

    [Fact] // UT-CFG-02
    public async Task GetInt_missing_key_returns_fallback()
        => (await Svc().GetIntAsync("t.missing", 7)).Should().Be(7);

    [Fact] // UT-CFG-03
    public async Task GetInt_unparseable_returns_fallback()
    {
        AddConfig("t.bad", "abc", ConfigValueType.Int);
        (await Svc().GetIntAsync("t.bad", 9)).Should().Be(9);
    }

    [Fact] // UT-CFG-04
    public async Task GetDecimal_invariant_culture()
    {
        AddConfig("t.dec", "20000000.50", ConfigValueType.Decimal);
        (await Svc().GetDecimalAsync("t.dec", 0m)).Should().Be(20000000.50m);
    }

    [Fact] // UT-CFG-05
    public async Task GetDecimal_vietnamese_format_returns_fallback()
    {
        AddConfig("t.vi", "20.000.000", ConfigValueType.Decimal);
        (await Svc().GetDecimalAsync("t.vi", -1m)).Should().Be(-1m);
    }

    [Theory] // UT-CFG-06
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("1", false)]   // bool.TryParse không nhận "1"
    public async Task GetBool_cases(string stored, bool expected)
    {
        AddConfig("t.bool", stored, ConfigValueType.Bool);
        (await Svc().GetBoolAsync("t.bool", false)).Should().Be(expected);
    }

    [Fact] // UT-CFG-07
    public async Task GetString_missing_key_returns_fallback()
        => (await Svc().GetStringAsync("t.none", "def")).Should().Be("def");

    [Fact] // UT-CFG-08
    public async Task Second_read_is_served_from_cache()
    {
        AddConfig("t.cached", "42", ConfigValueType.Int);
        var svc = Svc();

        (await svc.GetIntAsync("t.cached", 0)).Should().Be(42);

        // Xoá thẳng dưới DB (không qua service) — nếu còn đọc DB sẽ ra fallback.
        using (var other = _sql.NewContext())
        {
            var row = other.SystemConfigs.Single(c => c.ConfigKey == "t.cached");
            other.SystemConfigs.Remove(row);
            other.SaveChanges();
        }

        (await svc.GetIntAsync("t.cached", 0)).Should().Be(42);
    }

    [Fact] // UT-CFG-09
    public async Task After_the_cache_entry_expires_the_value_is_reloaded_from_db()
    {
        AddConfig("t.evict", "10", ConfigValueType.Int);
        var svc = Svc();
        (await svc.GetIntAsync("t.evict", 0)).Should().Be(10);

        using (var other = _sql.NewContext())
        {
            other.SystemConfigs.Single(c => c.ConfigKey == "t.evict").ConfigValue = "20";
            other.SaveChanges();
        }
        _cache.Remove("cfg:t.evict"); // mô phỏng hết TTL 5'

        (await svc.GetIntAsync("t.evict", 0)).Should().Be(20);
    }
}
