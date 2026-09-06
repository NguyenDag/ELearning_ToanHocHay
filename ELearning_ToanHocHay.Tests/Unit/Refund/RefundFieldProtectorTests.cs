using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Refund;

/// <summary>§3.23 — UT-PROT-*. Data Protection phù du (in-memory).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class RefundFieldProtectorTests
{
    private static RefundFieldProtector New() => new(DataProtection.Ephemeral());

    [Theory]
    [InlineData("0071000123456", "3456")]  // UT-PROT-01
    [InlineData("12-34 56 78", "5678")]     // UT-PROT-02
    [InlineData("123", "123")]              // UT-PROT-03
    [InlineData("", "")]                    // UT-PROT-04
    [InlineData(null, "")]                  // UT-PROT-04
    public void Last4(string? input, string expected)
        => New().Last4(input!).Should().Be(expected);

    [Fact] // UT-PROT-05
    public void Protect_then_Unprotect_round_trips()
    {
        var p = New();
        p.Unprotect(p.Protect("0071000123456")).Should().Be("0071000123456");
    }

    [Fact] // UT-PROT-06
    public void Protect_output_is_not_plaintext()
        => New().Protect("12345").Should().NotBe("12345");

    [Fact] // UT-PROT-07
    public void Unprotect_garbage_throws()
    {
        var act = () => New().Unprotect("not-a-ciphertext");
        act.Should().Throw<Exception>();
    }

    [Fact] // UT-PROT-08
    public void Different_key_rings_cannot_cross_decrypt()
    {
        var cipher = New().Protect("0071000123456");
        var act = () => New().Unprotect(cipher);
        act.Should().Throw<Exception>();
    }
}
