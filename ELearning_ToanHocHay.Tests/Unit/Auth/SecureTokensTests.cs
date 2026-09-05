using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.11 — UT-TOKEN-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class SecureTokensTests
{
    [Fact] // UT-TOKEN-01
    public void NewToken_is_url_safe()
        => SecureTokens.NewToken().Should().NotContainAny("+", "/", "=");

    [Fact] // UT-TOKEN-02
    public void NewToken_has_at_least_256_bits_base64url()
        => SecureTokens.NewToken().Length.Should().BeGreaterThanOrEqualTo(43);

    [Fact] // UT-TOKEN-03
    public void NewToken_is_random()
        => SecureTokens.NewToken().Should().NotBe(SecureTokens.NewToken());

    [Fact] // UT-TOKEN-04
    public void Hash_is_stable()
        => SecureTokens.Hash("some-raw-token").Should().Be(SecureTokens.Hash("some-raw-token"));

    [Fact] // UT-TOKEN-05
    public void Hash_differs_by_input()
        => SecureTokens.Hash("a").Should().NotBe(SecureTokens.Hash("b"));

    [Fact] // UT-TOKEN-06
    public void Hash_does_not_contain_plaintext()
    {
        const string raw = "plaintext-refresh-token-value";
        SecureTokens.Hash(raw).Should().NotContain(raw);
    }

    [Fact] // UT-TOKEN-07
    public void Hash_empty_string_is_valid_base64()
    {
        var hash = SecureTokens.Hash("");
        hash.Should().NotBeNullOrEmpty();
        var act = () => Convert.FromBase64String(hash);
        act.Should().NotThrow();
    }
}
