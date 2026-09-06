using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Refund;

/// <summary>§3.21 — UT-RFP-WINDOW-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class RefundDayWindowTests
{
    private static DateTime Utc(int y, int mo, int d, int h, int mi)
        => new(y, mo, d, h, mi, 0, DateTimeKind.Utc);

    [Fact] // UT-RFP-WINDOW-01
    public void Before_local_midnight_after_utc_midnight()
        => RefundDayWindow.StartOfDayUtc(Utc(2026, 9, 5, 2, 0), 7).Should().Be(Utc(2026, 9, 4, 17, 0));

    [Fact] // UT-RFP-WINDOW-02
    public void Same_local_day_evening()
        => RefundDayWindow.StartOfDayUtc(Utc(2026, 9, 5, 20, 0), 7).Should().Be(Utc(2026, 9, 5, 17, 0));

    [Fact] // UT-RFP-WINDOW-03
    public void Just_before_local_midnight()
        => RefundDayWindow.StartOfDayUtc(Utc(2026, 9, 5, 16, 59), 7).Should().Be(Utc(2026, 9, 4, 17, 0));

    [Fact] // UT-RFP-WINDOW-04
    public void Exactly_local_midnight()
        => RefundDayWindow.StartOfDayUtc(Utc(2026, 9, 5, 17, 0), 7).Should().Be(Utc(2026, 9, 5, 17, 0));

    [Fact] // UT-RFP-WINDOW-05
    public void Zero_offset_is_utc_midnight()
        => RefundDayWindow.StartOfDayUtc(Utc(2026, 9, 5, 13, 30), 0).Should().Be(Utc(2026, 9, 5, 0, 0));
}
