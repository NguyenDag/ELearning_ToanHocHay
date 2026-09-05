using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Sepay;

/// <summary>§3.15 — UT-SEPAY-AMOUNT-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class SePayAmountMatcherTests
{
    [Theory]
    [InlineData(199000, 199000, 0, true)]      // UT-SEPAY-AMOUNT-01
    [InlineData(199000, 198999, 0, false)]     // UT-SEPAY-AMOUNT-02
    [InlineData(199000, 198950, 100, true)]    // UT-SEPAY-AMOUNT-03
    [InlineData(199000, 198899, 100, false)]   // UT-SEPAY-AMOUNT-04
    [InlineData(199000, 199100, 100, true)]    // UT-SEPAY-AMOUNT-05 — overpay trong dung sai
    [InlineData(199000.4, 199000, 0, true)]    // UT-SEPAY-AMOUNT-06 — làm tròn expected
    [InlineData(199000, 0, 0, false)]          // UT-SEPAY-AMOUNT-07
    [InlineData(199000, 199000, -1, true)]     // UT-SEPAY-AMOUNT-08 — tolerance âm coi như 0
    public void Matches(double expected, double actual, double tolerance, bool result)
        => SePayAmountMatcher.Matches((decimal)expected, (decimal)actual, (decimal)tolerance)
            .Should().Be(result);
}
