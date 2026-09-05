using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Progress;

/// <summary>§3.31 — UT-PROG-* (phần U1: ngưỡng hoàn thành + công thức % roll-up).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class ProgressRollupTests
{
    [Fact] // UT-PROG-01
    public void Attempt_at_threshold_marks_lesson_complete()
        => ProgressRollup.IsLessonComplete(70m).Should().BeTrue();

    [Fact] // UT-PROG-02
    public void Attempt_below_threshold_does_not_complete()
        => ProgressRollup.IsLessonComplete(69m).Should().BeFalse();

    [Theory]
    [InlineData(3, 4, 75)]   // UT-PROG-03
    [InlineData(0, 4, 0)]    // UT-PROG-04
    [InlineData(4, 4, 100)]
    [InlineData(1, 3, 33.33)]
    [InlineData(0, 0, 0)]    // không có bài con
    public void PercentComplete(int completed, int total, decimal expected)
        => ProgressRollup.PercentComplete(completed, total).Should().Be(expected);
}
