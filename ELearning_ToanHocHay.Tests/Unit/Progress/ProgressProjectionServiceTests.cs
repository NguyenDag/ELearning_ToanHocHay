using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ELearning_ToanHocHay.Tests.Unit.Progress;

/// <summary>§3.31 — UT-PROG-06..09 (tầng U2, SQLite).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class ProgressProjectionServiceTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly int _studentId;
    private readonly int _cvId;

    public ProgressProjectionServiceTests()
    {
        _studentId = Seed.Student(_sql.Db).Student.StudentId;
        _cvId = Seed.CourseVersion(_sql.Db).CourseVersionId;
    }

    public void Dispose() => _sql.Dispose();

    private ProgressProjectionService Svc()
        => new(_sql.NewContext(), NullLogger<ProgressProjectionService>.Instance);

    private NodeProgress? Progress(int nodeId)
        => _sql.NewContext().NodeProgresses.SingleOrDefault(p => p.StudentId == _studentId && p.NodeId == nodeId);

    [Fact] // UT-PROG-06
    public async Task MarkLessonComplete_below_min_view_seconds_does_nothing()
    {
        var lesson = Seed.Node(_sql.Db, _cvId, NodeType.Lesson);

        var res = await Svc().MarkLessonCompleteAsync(_studentId, lesson.NodeId, secondsViewed: 5);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("20 giây");
        Progress(lesson.NodeId).Should().BeNull();
    }

    [Fact] // UT-PROG-07
    public async Task MarkLessonComplete_at_or_above_min_marks_100_percent()
    {
        var lesson = Seed.Node(_sql.Db, _cvId, NodeType.Lesson);

        var res = await Svc().MarkLessonCompleteAsync(_studentId, lesson.NodeId, secondsViewed: 30);

        res.Success.Should().BeTrue();
        var np = Progress(lesson.NodeId)!;
        np.Status.Should().Be(ProgressStatus.Completed);
        np.CompletionPercent.Should().Be(100m);
    }

    [Fact] // UT-PROG-08 — roll-up bài -> chương -> version cache
    public async Task Completing_a_lesson_rolls_up_to_chapter_and_course()
    {
        var chapter = Seed.Node(_sql.Db, _cvId, NodeType.Chapter);
        var lesson = Seed.Node(_sql.Db, _cvId, NodeType.Lesson, parent: chapter);
        _sql.Db.StudentCourses.Add(new StudentCourse
        {
            StudentId = _studentId,
            CourseId = _sql.Db.CourseVersions.Find(_cvId)!.CourseId,
            CourseVersionId = _cvId,
            Status = StudentCourseStatus.Active,
        });
        _sql.Db.SaveChanges();

        await Svc().MarkLessonCompleteAsync(_studentId, lesson.NodeId, secondsViewed: 30);

        Progress(lesson.NodeId)!.CompletionPercent.Should().Be(100m);
        Progress(chapter.NodeId)!.CompletionPercent.Should().Be(100m);
        _sql.NewContext().StudentCourses.Single(sc => sc.StudentId == _studentId)
            .ProgressPercent.Should().Be(100m);
    }

    [Fact] // UT-PROG-05 — roll-up dùng MaterializedPath.StartsWith("/…/id/"), không ghép "/id"
    public async Task RollUp_ignores_prefix_collisions_from_paths_that_share_a_leading_id()
    {
        var chapter = Seed.Node(_sql.Db, _cvId, NodeType.Chapter);
        var lesson = Seed.Node(_sql.Db, _cvId, NodeType.Lesson, parent: chapter);

        // "bẫy": bài của chương khác, path bắt đầu bằng "/{chapterId}" (số) nhưng KHÔNG phải hậu duệ.
        var trapChapter = Seed.Node(_sql.Db, _cvId, NodeType.Chapter);
        var trapLesson = Seed.Node(_sql.Db, _cvId, NodeType.Lesson, parent: trapChapter);
        using (var db = _sql.NewContext())
        {
            db.ContentNodes.Find(trapChapter.NodeId)!.MaterializedPath = $"/{chapter.NodeId}9/";
            db.ContentNodes.Find(trapLesson.NodeId)!.MaterializedPath = $"/{chapter.NodeId}9/{trapLesson.NodeId}/";
            db.SaveChanges();
        }

        // chỉ hoàn thành bài THẬT; bài bẫy để dở.
        await Svc().MarkLessonCompleteAsync(_studentId, lesson.NodeId, secondsViewed: 30);

        // đúng: chương thấy 1 bài con -> 100%. sai (ghép "/{id}"): thấy 2 bài -> 50%.
        Progress(chapter.NodeId)!.CompletionPercent.Should().Be(100m);
    }

    [Fact] // UT-PROG-09
    public async Task ProjectAttempt_ignores_an_in_progress_attempt()
    {
        var exercise = Seed.Exercise(_sql.Db, nodeId: null);
        var attempt = new ExerciseAttempt
        {
            StudentId = _studentId,
            ExerciseId = exercise.ExerciseId,
            Status = AttemptStatus.InProgress,
            StartTime = DateTime.UtcNow,
        };
        _sql.Db.ExerciseAttempts.Add(attempt);
        _sql.Db.SaveChanges();

        await Svc().ProjectAttemptAsync(attempt.AttemptId);

        _sql.NewContext().DailyActivitySnapshots.Should().BeEmpty();
        _sql.NewContext().NodeProgresses.Should().BeEmpty();
    }
}
