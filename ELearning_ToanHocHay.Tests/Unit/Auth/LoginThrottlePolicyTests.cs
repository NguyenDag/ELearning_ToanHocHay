using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.8 — UT-THROTTLE-*. Công thức khoá leo thang, thuần.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class LoginThrottlePolicyTests
{
    [Theory]
    [InlineData(1, null)]    // UT-THROTTLE-01
    [InlineData(4, null)]    // UT-THROTTLE-02
    [InlineData(5, 1)]       // UT-THROTTLE-03
    [InlineData(6, 2)]       // UT-THROTTLE-04
    [InlineData(7, 4)]       // UT-THROTTLE-05
    [InlineData(8, 8)]       // UT-THROTTLE-06
    [InlineData(9, 16)]      // UT-THROTTLE-07
    [InlineData(10, 30)]     // UT-THROTTLE-08 — cap, không phải 32
    [InlineData(50, 30)]     // UT-THROTTLE-09
    [InlineData(0, null)]    // UT-THROTTLE-10
    [InlineData(-3, null)]   // UT-THROTTLE-10
    public void NextLockout(int failedCount, int? expectedMinutes)
    {
        var result = LoginThrottlePolicy.NextLockout(failedCount);

        if (expectedMinutes is null)
            result.Should().BeNull();
        else
            result.Should().Be(TimeSpan.FromMinutes(expectedMinutes.Value));
    }
}
