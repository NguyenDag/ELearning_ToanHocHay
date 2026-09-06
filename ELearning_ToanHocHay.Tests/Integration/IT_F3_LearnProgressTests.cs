using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F3 — Học bài (bài giảng) &amp; tiến độ (IT-F3).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Progress")]
public class IT_F3_LearnProgressTests : IntegrationTest
{
    public IT_F3_LearnProgressTests(ApiFactory app) : base(app) { }

    private Task<HttpResponseMessage> Complete(HttpClient client, int nodeId, int seconds)
        => client.PostAsJsonAsync($"/api/progress/lessons/{nodeId}/complete", new { SecondsViewed = seconds });

    [SkippableFact] // IT-F3-02
    public async Task IT_F3_02_Paid_lesson_complete_without_enrolment_is_403()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var c = await Flow.PublishCourseAsync();

        var res = await Complete(App.As(userId), c.FirstPaidLessonId, 60);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F3-03
    public async Task IT_F3_03_Enrolled_student_marks_lesson_complete()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        var c = await Flow.PublishCourseAsync();
        await Flow.EnrolAsync(userId, c.CourseId, c.VersionId);

        var res = await Complete(App.As(userId), c.FirstPaidLessonId, 45);
        await res.ShouldBeOk();

        await App.Db(async db =>
            (await db.NodeProgresses.SingleAsync(p => p.StudentId == studentId && p.NodeId == c.FirstPaidLessonId))
                .CompletionPercent.Should().Be(100m));
    }

    [SkippableFact] // IT-F3-04
    public async Task IT_F3_04_Too_little_view_time_does_not_complete()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        var c = await Flow.PublishCourseAsync();
        await Flow.EnrolAsync(userId, c.CourseId, c.VersionId);

        var res = await Complete(App.As(userId), c.FirstPaidLessonId, 5);

        res.IsSuccessStatusCode.Should().BeFalse();
        await App.Db(async db =>
            (await db.NodeProgresses.AnyAsync(p => p.StudentId == studentId && p.NodeId == c.FirstPaidLessonId))
                .Should().BeFalse());
    }

    [SkippableFact] // IT-F3-05
    public async Task IT_F3_05_Completing_a_lesson_rolls_up_to_chapter_and_version_cache()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        var c = await Flow.PublishCourseAsync();
        await Flow.EnrolAsync(userId, c.CourseId, c.VersionId);

        await (await Complete(App.As(userId), c.FirstPaidLessonId, 45)).ShouldBeOk();

        await App.Db(async db =>
        {
            (await db.NodeProgresses.SingleAsync(p => p.StudentId == studentId && p.NodeId == c.FreeChapterIds[0]))
                .CompletionPercent.Should().BeGreaterThan(0m);
            (await db.StudentCourses.SingleAsync(sc => sc.StudentId == studentId && sc.CourseId == c.CourseId))
                .ProgressPercent.Should().BeGreaterThan(0m);
        });
    }

    [SkippableFact] // IT-F3-06
    public async Task IT_F3_06_Version_progress_endpoint()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var c = await Flow.PublishCourseAsync();
        await Flow.EnrolAsync(userId, c.CourseId, c.VersionId);
        var client = App.As(userId);
        await (await Complete(client, c.FreeLessonId, 45)).ShouldBeOk();

        var res = await client.GetAsync($"/api/progress/versions/{c.VersionId}");
        await res.ShouldBeOk();
        var items = await res.DataAsync();
        items.EnumerateArray().Select(x => x.GetProperty("NodeId").GetInt32()).Should().Contain(c.FreeLessonId);
    }

    [SkippableFact] // IT-F3-07
    public async Task IT_F3_07_Lesson_outside_enrolled_course_is_403()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var enrolled = await Flow.PublishCourseAsync();
        await Flow.EnrolAsync(userId, enrolled.CourseId, enrolled.VersionId);
        var other = await Flow.PublishCourseAsync();

        var res = await Complete(App.As(userId), other.FirstPaidLessonId, 60);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F3-08
    public async Task IT_F3_08_Heatmap_is_owner_guarded()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.StudentB).GetAsync($"/api/progress/students/{Ids.StudentAId}/heatmap");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F3-09
    public async Task IT_F3_09_Owner_reads_own_heatmap()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        await App.Db(async db =>
        {
            db.DailyActivitySnapshots.Add(new DailyActivitySnapshot
            {
                StudentId = studentId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                MinutesStudied = 30, ExercisesDone = 1, LessonsDone = 2, QuestionsAnswered = 4,
            });
            await db.SaveChangesAsync();
        });

        var res = await App.As(userId).GetAsync($"/api/progress/students/{studentId}/heatmap");
        await res.ShouldBeOk();
        (await res.DataAsync()).GetArrayLength().Should().BeGreaterThan(0);
    }
}
