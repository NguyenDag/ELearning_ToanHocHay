using Microsoft.Extensions.Configuration;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// <see cref="IConfiguration"/> in-memory cho các unit test cần đọc cấu hình
/// (JwtService, SePayService, RateLimitPartitioning…). <see cref="Jwt"/> là preset
/// hợp lệ tối thiểu (SecretKey ≥ 32 ký tự) — §3.9.
/// </summary>
public static class TestConfig
{
    public static IConfiguration Build(params (string key, string val)[] overrides)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (key, val) in overrides)
            dict[key] = val;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    /// <summary>Preset JwtSettings hợp lệ. Truyền thêm override để đổi từng key.</summary>
    public static IConfiguration Jwt(params (string key, string val)[] overrides)
    {
        var baseRows = new (string, string)[]
        {
            ("JwtSettings:SecretKey", "unit-test-signing-key-0123456789-abcdefghij"),
            ("JwtSettings:Issuer", "ut-issuer"),
            ("JwtSettings:Audience", "ut-audience"),
            ("JwtSettings:ExpirationMinutes", "60"),
        };

        var merged = new Dictionary<string, string>();
        foreach (var (k, v) in baseRows) merged[k] = v;
        foreach (var (k, v) in overrides) merged[k] = v;

        return Build(merged.Select(kv => (kv.Key, kv.Value)).ToArray());
    }
}
