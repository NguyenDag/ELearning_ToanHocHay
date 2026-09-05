using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Sepay;

/// <summary>§3.14 — UT-SEPAY-PARSE-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class SePayContentParserTests
{
    [Theory]
    [InlineData("TKPTTS SUBSCRIPTION_12", 12)]                    // UT-SEPAY-PARSE-01
    [InlineData("SUBSCRIPTION-7", 7)]                             // UT-SEPAY-PARSE-02
    [InlineData("SUBSCRIPTION7", 7)]                              // UT-SEPAY-PARSE-03
    [InlineData("abc SUBSCRIPTION_3 xyz", 3)]                     // UT-SEPAY-PARSE-04
    [InlineData("subscription_5", 5)]                             // UT-SEPAY-PARSE-05 — IgnoreCase (chốt)
    [InlineData("SUBSCRIPTION_012", 12)]                          // UT-SEPAY-PARSE-10
    [InlineData("SUBSCRIPTION_1 SUBSCRIPTION_2", 1)]              // UT-SEPAY-PARSE-11 — khớp đầu tiên
    public void TryParseSubscriptionId_hits(string content, int expected)
        => SePayContentParser.TryParseSubscriptionId(content).Should().Be(expected);

    [Theory]
    [InlineData("chuyen tien hoc phi")]                           // UT-SEPAY-PARSE-06
    [InlineData("SUBSCRIPTION_")]                                 // UT-SEPAY-PARSE-07
    [InlineData("")]                                              // UT-SEPAY-PARSE-08
    [InlineData(null)]                                            // UT-SEPAY-PARSE-08
    [InlineData("   ")]                                           // UT-SEPAY-PARSE-08
    [InlineData("SUBSCRIPTION_99999999999999999999")]            // UT-SEPAY-PARSE-09 — tràn int
    public void TryParseSubscriptionId_misses(string? content)
        => SePayContentParser.TryParseSubscriptionId(content).Should().BeNull();
}
