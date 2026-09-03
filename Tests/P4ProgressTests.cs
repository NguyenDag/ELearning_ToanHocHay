using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// P4 — progress projection (NodeProgress + roll-up), lesson completion,
/// activity heatmap, and A2-05 (tier from Package.Tier).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class P4ProgressTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public P4ProgressTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    private sealed record CourseFixture(int CourseId, int VersionId, int ChapterId, int Lesson1Id, int Lesson2Id, int ExerciseId);

    /// <summary>Published course: 1 chapter, 2 lessons, an exercise (1 MC question) on lesson 1. Student A enrolled.</summary>
    private async Task<CourseFixture> BuildCourseAsync(string tag)
    {
        return await _f.QueryDbAsync(async db =>
        {
            var subjectId = await db.Subjects.Where(s => s.Code == "MATH").Select(s => s.SubjectId).FirstAsync();
            var gradeId = await db.GradeLevels.Where(g => g.Code == "G6").Select(g => g.GradeLevelId).FirstAsync();

            var fw = new CurriculumFramework { Code = $"P4-{tag}", Name = $"P4 {tag}" };
            db.CurriculumFrameworks.Add(fw);
            await db.SaveChangesAsync();

            var course = new Course
            {
                SubjectId = subjectId, GradeLevelId = gradeId, FrameworkId = fw.FrameworkId,
                Title = $"P4 {tag}", Slug = $"p4-{tag}", Status = CourseStatus.Published,
                CreatedBy = Id.ContentEditorUserId
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var version = new CourseVersion
            {
                CourseId = course.CourseId, VersionNumber = 1, State = VersionState.Published,
                PublishedAt = DateTime.UtcNow
            };
            db.CourseVersions.Add(version);
            await db.SaveChangesAsync();

            var chapter = new ContentNode
            {
                CourseVersionId = version.CourseVersionId, NodeType = NodeType.Chapter,
                Title = "Ch1", OrderIndex = 0, Depth = 0, MaterializedPath = "/", CreatedBy = Id.ContentEditorUserId
            };
            db.ContentNodes.Add(chapter);
            await db.SaveChangesAsync();
            chapter.MaterializedPath = $"/{chapter.NodeId}/";

            ContentNode Lesson(string title, int order) => new()
            {
                CourseVersionId = version.CourseVersionId, ParentNodeId = chapter.NodeId, NodeType = NodeType.Lesson,
                Title = title, OrderIndex = order, Depth = 1, MaterializedPath = "/", IsFree = false,
                CreatedBy = Id.ContentEditorUserId
            };
            var l1 = Lesson("L1", 0);
            var l2 = Lesson("L2", 1);
            db.ContentNodes.AddRange(l1, l2);
            await db.SaveChangesAsync();
            l1.MaterializedPath = $"{chapter.MaterializedPath}{l1.NodeId}/";
            l2.MaterializedPath = $"{chapter.MaterializedPath}{l2.NodeId}/";
            await db.SaveChangesAsync();

            var exercise = new Exercise
            {
                NodeId = l1.NodeId, ExerciseName = $"P4 ex {tag}", ExerciseType = ExerciseType.Quiz,
                Status = ExerciseStatus.Published, IsActive = true, IsFree = true,
                TotalQuestions = 1, TotalScores = 1, PassingScore = 1, CreatedBy = Id.ContentEditorUserId
            };
            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            db.ExerciseQuestions.Add(new ExerciseQuestion
            {
                ExerciseId = exercise.ExerciseId, QuestionId = Id.McQuestionId, Score = 1, OrderIndex = 1
            });

            db.StudentCourses.Add(new StudentCourse
            {
                StudentId = Id.StudentAId, CourseId = course.CourseId, CourseVersionId = version.CourseVersionId,
                Source = EnrollSource.Self, Status = StudentCourseStatus.Active, EnrolledAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            return new CourseFixture(course.CourseId, version.CourseVersionId, chapter.NodeId, l1.NodeId, l2.NodeId, exercise.ExerciseId);
        });
    }

    private async Task SubmitCorrectAttemptAsync(int exerciseId)
    {
        var client = _f.ClientFor(Id.StudentAUserId);

        var start = await client.PostAsJsonAsync("/api/exerciseattempts/start", new { ExerciseId = exerciseId });
        start.StatusCode.Should().Be(HttpStatusCode.OK, await start.Content.ReadAsStringAsync());
        var attemptId = (await Root(start)).GetProperty("Data").GetProperty("AttemptId").GetInt32();

        var save = await client.PostAsJsonAsync("/api/exerciseattempts/save-answer",
            new { AttemptId = attemptId, QuestionId = Id.McQuestionId, SelectedOptionId = Id.McCorrectOptionId });
        save.StatusCode.Should().Be(HttpStatusCode.OK, await save.Content.ReadAsStringAsync());

        var complete = await client.PostAsJsonAsync("/api/exerciseattempts/complete", new { AttemptId = attemptId });
        complete.StatusCode.Should().Be(HttpStatusCode.OK, await complete.Content.ReadAsStringAsync());
    }

    [SkippableFact]
    public async Task Submitting_an_attempt_writes_NodeProgress_and_rolls_up()
    {
        RequireDocker();
        var fx = await BuildCourseAsync("rollup");

        await SubmitCorrectAttemptAsync(fx.ExerciseId);

        var lesson = await _f.QueryDbAsync(db => db.NodeProgresses.AsNoTracking()
            .SingleAsync(p => p.StudentId == Id.StudentAId && p.NodeId == fx.Lesson1Id));
        lesson.Status.Should().Be(ProgressStatus.Completed);
        lesson.TotalAttempts.Should().Be(1);

        var chapter = await _f.QueryDbAsync(db => db.NodeProgresses.AsNoTracking()
            .SingleAsync(p => p.StudentId == Id.StudentAId && p.NodeId == fx.ChapterId));
        chapter.CompletionPercent.Should().Be(50m); // 1 of 2 lessons
        chapter.Status.Should().Be(ProgressStatus.InProgress);
    }

    [SkippableFact]
    public async Task Mark_lesson_complete_needs_view_time_then_rolls_up_to_100()
    {
        RequireDocker();
        var fx = await BuildCourseAsync("markdone");
        await SubmitCorrectAttemptAsync(fx.ExerciseId); // lesson 1 done

        var client = _f.ClientFor(Id.StudentAUserId);

        (await client.PostAsJsonAsync($"/api/progress/lessons/{fx.Lesson2Id}/complete", new { SecondsViewed = 3 }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await client.PostAsJsonAsync($"/api/progress/lessons/{fx.Lesson2Id}/complete", new { SecondsViewed = 45 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var chapter = await _f.QueryDbAsync(db => db.NodeProgresses.AsNoTracking()
            .SingleAsync(p => p.StudentId == Id.StudentAId && p.NodeId == fx.ChapterId));
        chapter.CompletionPercent.Should().Be(100m);
        chapter.Status.Should().Be(ProgressStatus.Completed);

        var enrol = await _f.QueryDbAsync(db => db.StudentCourses.AsNoTracking()
            .SingleAsync(sc => sc.StudentId == Id.StudentAId && sc.CourseVersionId == fx.VersionId));
        enrol.ProgressPercent.Should().Be(100m);
        enrol.Status.Should().Be(StudentCourseStatus.Completed);
    }

    [SkippableFact]
    public async Task Heatmap_is_owner_guarded()
    {
        RequireDocker();

        (await _f.ClientFor(Id.StudentBUserId).GetAsync($"/api/progress/students/{Id.StudentAId}/heatmap"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _f.ClientFor(Id.StudentAUserId).GetAsync($"/api/progress/students/{Id.StudentAId}/heatmap"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // a linked parent may look
        (await _f.ClientFor(Id.ParentLinkedUserId).GetAsync($"/api/progress/students/{Id.StudentAId}/heatmap"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Dashboard_tier_comes_from_Package_Tier_not_the_name()
    {
        RequireDocker();

        // seed subscription is Pending -> student A is Free -> Standard-only endpoint is 403
        (await _f.ClientFor(Id.StudentAUserId)
            .GetAsync($"/api/student/{Id.StudentAId}/dashboard/chapter-score-comparison"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _f.QueryDbAsync(async db =>
        {
            var sub = await db.Subscriptions.SingleAsync(s => s.SubscriptionId == Id.SubscriptionAId);
            sub.Status = SubscriptionStatus.Active;
            sub.EndDate = DateTime.UtcNow.AddDays(30);
            await db.SaveChangesAsync();
            return true;
        });

        // package tier is Standard -> now allowed
        (await _f.ClientFor(Id.StudentAUserId)
            .GetAsync($"/api/student/{Id.StudentAId}/dashboard/chapter-score-comparison"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // reset so other tests keep seeing a Free student A
        await _f.QueryDbAsync(async db =>
        {
            var sub = await db.Subscriptions.SingleAsync(s => s.SubscriptionId == Id.SubscriptionAId);
            sub.Status = SubscriptionStatus.Pending;
            await db.SaveChangesAsync();
            return true;
        });
    }
}
