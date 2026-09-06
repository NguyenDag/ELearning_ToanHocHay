using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F2 — Danh mục &amp; ghi danh (IT-F2).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Content")]
public class IT_F2_CatalogEnrollmentTests : IntegrationTest
{
    public IT_F2_CatalogEnrollmentTests(ApiFactory app) : base(app) { }

    private Task<int> StudentIdOf(int userId)
        => App.Db(db => db.Students.Where(s => s.UserId == userId).Select(s => s.StudentId).FirstAsync());

    private static IEnumerable<int> NodeIds(JsonElement tree)
    {
        foreach (var n in tree.EnumerateArray())
        {
            yield return n.GetProperty("NodeId").GetInt32();
            if (n.TryGetProperty("Children", out var kids) && kids.ValueKind == JsonValueKind.Array)
                foreach (var id in NodeIds(kids)) yield return id;
        }
    }

    [SkippableFact] // IT-F2-01
    public async Task IT_F2_01_Subjects_are_public()
    {
        RequireDocker();
        await (await App.Anonymous().GetAsync("/api/catalog/subjects")).ShouldBeOk();
    }

    [SkippableFact] // IT-F2-03
    public async Task IT_F2_03_Student_cannot_create_a_subject()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.StudentA)
            .PostAsJsonAsync("/api/catalog/subjects", new { Code = "X", Name = "X", Slug = "x" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F2-04
    public async Task IT_F2_04_Unknown_course_slug_is_404()
    {
        RequireDocker();
        var res = await App.Anonymous().GetAsync($"/api/courses/by-slug/khong-ton-tai-{Guid.NewGuid():N}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact] // IT-F2-05
    public async Task IT_F2_05_Anonymous_content_tree_is_free_only()
    {
        RequireDocker();
        var c = await Flow.PublishCourseAsync();

        var res = await App.Anonymous().GetAsync($"/api/learn/courses/{c.CourseId}/content");
        await res.ShouldBeOk();
        var data = await res.DataAsync();

        data.GetProperty("AccessLevel").GetString().Should().Be("FreeOnly");
        var ids = NodeIds(data.GetProperty("Tree")).ToList();
        ids.Should().Contain(c.FreeChapterIds[0]);
        ids.Should().NotContain(c.FirstPaidLessonId);
    }

    [SkippableFact] // IT-F2-06
    public async Task IT_F2_06_Paid_lesson_is_forbidden_without_access()
    {
        RequireDocker();
        var c = await Flow.PublishCourseAsync();

        var res = await App.AsRole(TestRole.StudentB).GetAsync($"/api/learn/nodes/{c.FirstPaidLessonId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F2-07
    public async Task IT_F2_07_Enroll_then_list_my_enrolments()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var c = await Flow.PublishCourseAsync();
        var client = App.As(userId);

        var enroll = await client.PostAsync($"/api/enrollments/courses/{c.CourseId}", null);
        await enroll.ShouldBeOk();

        var mine = await client.GetAsync("/api/enrollments/me");
        var data = await mine.DataAsync();
        data.EnumerateArray().Select(e => e.GetProperty("CourseId").GetInt32()).Should().Contain(c.CourseId);

        var studentId = await StudentIdOf(userId);
        await App.Db(async db =>
            (await db.StudentCourses.CountAsync(sc => sc.StudentId == studentId && sc.CourseId == c.CourseId))
                .Should().Be(1));
    }

    [SkippableFact] // IT-F2-08
    public async Task IT_F2_08_Enrolled_student_gets_full_access()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var c = await Flow.PublishCourseAsync();
        await Flow.EnrolAsync(userId, c.CourseId, c.VersionId);
        var client = App.As(userId);

        var content = await client.GetAsync($"/api/learn/courses/{c.CourseId}/content");
        (await content.DataAsync()).GetProperty("AccessLevel").GetString().Should().Be("Full");

        var node = await client.GetAsync($"/api/learn/nodes/{c.FirstPaidLessonId}");
        await node.ShouldBeOk();
    }

    [SkippableFact] // IT-F2-09
    public async Task IT_F2_09_SubjectGrade_entitlement_unlocks_the_paid_lesson()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var studentId = await StudentIdOf(userId);
        var c = await Flow.PublishCourseAsync();
        await Flow.GrantEntitlementAsync(studentId, EntitlementScope.SubjectGrade, subjectId: 1, gradeId: 1);

        var res = await App.As(userId).GetAsync($"/api/learn/nodes/{c.FirstPaidLessonId}");
        await res.ShouldBeOk();
    }

    [SkippableFact] // IT-F2-10
    public async Task IT_F2_10_Entitlement_for_another_subject_does_not_unlock()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var studentId = await StudentIdOf(userId);
        var c = await Flow.PublishCourseAsync();
        var otherSubjectId = await App.Db(async db =>
        {
            var s = new Subject
            {
                Code = $"S{Guid.NewGuid():N}"[..8], Name = "Môn khác",
                Slug = $"mon-khac-{Guid.NewGuid():N}", ColorHex = "#000000", DisplayOrder = 99, IsActive = true,
            };
            db.Subjects.Add(s);
            await db.SaveChangesAsync();
            return s.SubjectId;
        });
        await Flow.GrantEntitlementAsync(studentId, EntitlementScope.Subject, subjectId: otherSubjectId);

        var res = await App.As(userId).GetAsync($"/api/learn/nodes/{c.FirstPaidLessonId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F2-11
    public async Task IT_F2_11_Cannot_enroll_in_a_draft_course()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var draftId = await App.Db(async db =>
        {
            var course = new Course
            {
                SubjectId = 1, GradeLevelId = 1, Title = "Draft", Slug = $"draft-{Guid.NewGuid():N}",
                Status = CourseStatus.Draft, CreatedBy = 1,
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            return course.CourseId;
        });

        var res = await App.As(userId).PostAsync($"/api/enrollments/courses/{draftId}", null);
        res.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [SkippableFact] // IT-F2-12
    public async Task IT_F2_12_Enrolling_twice_is_idempotent()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var c = await Flow.PublishCourseAsync();
        var client = App.As(userId);

        await (await client.PostAsync($"/api/enrollments/courses/{c.CourseId}", null)).ShouldBeOk();
        var second = await client.PostAsync($"/api/enrollments/courses/{c.CourseId}", null);
        await second.ShouldBeOk();
        (await second.RootAsync()).GetProperty("Message").GetString().Should().Contain("Already enrolled");

        var studentId = await StudentIdOf(userId);
        await App.Db(async db =>
            (await db.StudentCourses.CountAsync(sc => sc.StudentId == studentId && sc.CourseId == c.CourseId))
                .Should().Be(1));
    }

    [SkippableFact] // IT-F2-13
    public async Task IT_F2_13_Student_cannot_author_or_read_question_banks()
    {
        RequireDocker();
        var client = App.AsRole(TestRole.StudentA);

        (await client.PostAsJsonAsync("/api/courses", new { SubjectId = 1, GradeLevelId = 1, Title = "x", Slug = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/question-banks")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
