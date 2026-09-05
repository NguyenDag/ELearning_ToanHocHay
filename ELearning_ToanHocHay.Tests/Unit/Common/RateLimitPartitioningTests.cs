using System.Net;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ELearning_ToanHocHay.Tests.Unit.Common;

/// <summary>§3.18 — UT-RLP-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class RateLimitPartitioningTests
{
    private static HttpContext Ctx(string? remoteIp, string? clientKeyHeader = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = remoteIp is null ? null : IPAddress.Parse(remoteIp);
        if (clientKeyHeader is not null)
            ctx.Request.Headers[RateLimitPartitioning.ClientKeyHeader] = clientKeyHeader;
        return ctx;
    }

    private static IConfiguration TrustedProxies(params string[] proxies)
    {
        var rows = proxies.Select((p, i) => ($"RateLimiting:TrustedProxies:{i}", p)).ToArray();
        return TestConfig.Build(rows);
    }

    [Fact] // UT-RLP-01
    public void Loopback_with_header_uses_client_key()
        => RateLimitPartitioning.ResolveClientKey(Ctx("127.0.0.1", "1.2.3.4"), TestConfig.Build())
            .Should().Be("ck:1.2.3.4");

    [Fact] // UT-RLP-02
    public void Loopback_without_header_uses_ip()
        => RateLimitPartitioning.ResolveClientKey(Ctx("127.0.0.1"), TestConfig.Build())
            .Should().Be("ip:127.0.0.1");

    [Fact] // UT-RLP-03
    public void Untrusted_remote_ignores_header()
        => RateLimitPartitioning.ResolveClientKey(Ctx("203.0.113.9", "1.2.3.4"), TestConfig.Build())
            .Should().Be("ip:203.0.113.9");

    [Fact] // UT-RLP-04
    public void Client_key_is_truncated_to_100_chars()
    {
        var longKey = new string('x', 150);
        RateLimitPartitioning.ResolveClientKey(Ctx("127.0.0.1", longKey), TestConfig.Build())
            .Should().Be("ck:" + new string('x', 100));
    }

    [Fact] // UT-RLP-05
    public void Configured_trusted_proxy_is_trusted()
        => RateLimitPartitioning.ResolveClientKey(Ctx("10.0.0.5", "1.2.3.4"), TrustedProxies("10.0.0.5"))
            .Should().Be("ck:1.2.3.4");

    [Fact] // UT-RLP-06
    public void Remote_not_in_trusted_list_ignores_header()
        => RateLimitPartitioning.ResolveClientKey(Ctx("10.0.0.6", "1.2.3.4"), TrustedProxies("10.0.0.5"))
            .Should().Be("ip:10.0.0.6");

    [Fact] // UT-RLP-07
    public void Ipv4_mapped_ipv6_loopback_is_trusted()
        => RateLimitPartitioning.ResolveClientKey(Ctx("::ffff:127.0.0.1", "1.2.3.4"), TestConfig.Build())
            .Should().Be("ck:1.2.3.4");

    [Fact] // UT-RLP-08
    public void Null_remote_ip_is_unknown()
        => RateLimitPartitioning.ResolveClientKey(Ctx(null), TestConfig.Build())
            .Should().Be("ip:unknown");

    [Fact] // UT-RLP-09
    public void Empty_header_value_falls_back_to_ip()
        => RateLimitPartitioning.ResolveClientKey(Ctx("127.0.0.1", ""), TestConfig.Build())
            .Should().Be("ip:127.0.0.1");

    [Fact] // UT-RLP-10
    public void Without_configured_proxies_only_loopback_is_trusted()
    {
        RateLimitPartitioning.ResolveClientKey(Ctx("127.0.0.1", "1.2.3.4"), TestConfig.Build())
            .Should().Be("ck:1.2.3.4");
        RateLimitPartitioning.ResolveClientKey(Ctx("10.0.0.9", "1.2.3.4"), TestConfig.Build())
            .Should().Be("ip:10.0.0.9");
    }
}
