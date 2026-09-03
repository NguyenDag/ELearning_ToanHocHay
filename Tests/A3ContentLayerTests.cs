using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// A3/P2 — content layer (catalog, course + version workflow, content tree, gate,
/// enrolment, question-bank workflow).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class A3ContentLayerTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public A3ContentLayerTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    private static JsonElement Data(JsonElement root) => root.GetProperty("Data");

    private async Task<int> SubjectId() =>
        await _f.QueryDbAsync(db => db.Subjects.AsNoTracking().Where(s => s.Code == "MATH")
            .Select(s => s.SubjectId).FirstAsync());

    private async Task<int> GradeId() =>
        await _f.QueryDbAsync(db => db.GradeLevels.AsNoTracking().Where(g => g.Code == "G6")
            .Select(g => g.GradeLevelId).FirstAsync());

    /// <summary>Creates a course with one chapter + one lesson and returns (courseId, versionId, chapterId, lessonId).</summary>
    private async Task<(int courseId, int versionId, int chapterId, int lessonId)> CreateDraftCourseAsync(string slug)
    {
        var editor = _f.ClientFor(Id.ContentEditorUserId);

        // A distinct framework per course keeps the (subject, grade, framework) index happy.
        var fwRes = await editor.PostAsJsonAsync("/api/catalog/frameworks", new
        {
            Code = $"FW-{slug}",
            Name = $"Framework {slug}"
        });
        fwRes.StatusCode.Should().Be(HttpStatusCode.OK, await fwRes.Content.ReadAsStringAsync());
        var frameworkId = Data(await Root(fwRes)).GetProperty("FrameworkId").GetInt32();

        var courseRes = await editor.PostAsJsonAsync("/api/courses", new
        {
            SubjectId = await SubjectId(),
            GradeLevelId = await GradeId(),
            FrameworkId = frameworkId,
            Title = $"Course {slug}",
            Slug = slug,
            ListPrice = 100000m
        });
        courseRes.StatusCode.Should().Be(HttpStatusCode.OK, await courseRes.Content.ReadAsStringAsync());
        var course = Data(await Root(courseRes));
        var courseId = course.GetProperty("CourseId").GetInt32();
        var versionId = course.GetProperty("Versions")[0].GetProperty("CourseVersionId").GetInt32();

        var chapterRes = await editor.PostAsJsonAsync($"/api/content/versions/{versionId}/nodes", new
        {
            NodeType = (int)NodeType.Chapter,
            Title = "Chapter 1",
            IsFree = true
        });
        chapterRes.StatusCode.Should().Be(HttpStatusCode.OK, await chapterRes.Content.ReadAsStringAsync());
        var chapterId = Data(await Root(chapterRes)).GetProperty("NodeId").GetInt32();

        var lessonRes = await editor.PostAsJsonAsync($"/api/content/versions/{versionId}/nodes", new
        {
            ParentNodeId = chapterId,
            NodeType = (int)NodeType.Lesson,
            Title = "Lesson 1.1",
            IsFree = false
        });
        lessonRes.StatusCode.Should().Be(HttpStatusCode.OK, await lessonRes.Content.ReadAsStringAsync());
        var lessonId = Data(await Root(lessonRes)).GetProperty("NodeId").GetInt32();

        return (courseId, versionId, chapterId, lessonId);
    }

    private async Task PublishAsync(int versionId)
    {
        var editor = _f.ClientFor(Id.ContentEditorUserId);
        var admin = _f.ClientFor(Id.AdminUserId);

        (await editor.PostAsync($"/api/courses/versions/{versionId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PostAsJsonAsync($"/api/courses/versions/{versionId}/review", new { Decision = (int)ReviewDecision.Approve }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PostAsync($"/api/courses/versions/{versionId}/publish", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------------- catalog authorization ----------------

    [SkippableFact]
    public async Task Catalog_subjects_are_public_but_writes_need_a_content_role()
    {
        RequireDocker();

        (await _f.CreateClient().GetAsync("/api/catalog/subjects"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await _f.ClientFor(Id.StudentAUserId).PostAsJsonAsync("/api/catalog/subjects",
                new { Code = "PHYS", Name = "Physics", Slug = "ly" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------- course version workflow ----------------

    [SkippableFact]
    public async Task Version_lifecycle_draft_to_published_flips_course_status()
    {
        RequireDocker();
        var (courseId, versionId, _, _) = await CreateDraftCourseAsync("lifecycle-course");

        await PublishAsync(versionId);

        var state = await _f.QueryDbAsync(db => db.CourseVersions.AsNoTracking()
            .Where(v => v.CourseVersionId == versionId).Select(v => v.State).FirstAsync());
        state.Should().Be(VersionState.Published);

        var status = await _f.QueryDbAsync(db => db.Courses.AsNoTracking()
            .Where(c => c.CourseId == courseId).Select(c => c.Status).FirstAsync());
        status.Should().Be(CourseStatus.Published);
    }

    [SkippableFact]
    public async Task Content_cannot_be_edited_after_the_version_is_published()
    {
        RequireDocker();
        var (_, versionId, _, _) = await CreateDraftCourseAsync("locked-course");
        await PublishAsync(versionId);

        var res = await _f.ClientFor(Id.ContentEditorUserId)
            .PostAsJsonAsync($"/api/content/versions/{versionId}/nodes",
                new { NodeType = (int)NodeType.Chapter, Title = "Late chapter" });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Root(res)).GetProperty("Message").GetString().Should().Contain("Draft");
    }

    // ---------------- node type rules ----------------

    [SkippableFact]
    public async Task A_lesson_cannot_be_created_directly_under_the_root()
    {
        RequireDocker();
        var (_, versionId, _, _) = await CreateDraftCourseAsync("rules-course");

        var res = await _f.ClientFor(Id.ContentEditorUserId)
            .PostAsJsonAsync($"/api/content/versions/{versionId}/nodes",
                new { NodeType = (int)NodeType.Lesson, Title = "Orphan lesson" });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------- the content gate ----------------

    [SkippableFact]
    public async Task Non_entitled_viewers_only_see_free_nodes()
    {
        RequireDocker();
        var (courseId, versionId, chapterId, lessonId) = await CreateDraftCourseAsync("gated-course");
        await PublishAsync(versionId);

        // anonymous: free chapter visible, paid lesson trimmed out
        var anon = await _f.CreateClient().GetAsync($"/api/learn/courses/{courseId}/content");
        anon.StatusCode.Should().Be(HttpStatusCode.OK);
        var tree = Data(await Root(anon)).GetProperty("Tree");
        tree.GetArrayLength().Should().Be(1);
        tree[0].GetProperty("NodeId").GetInt32().Should().Be(chapterId);
        tree[0].GetProperty("Children").GetArrayLength().Should().Be(0);

        // paid node directly -> 403 for a student with no entitlement
        (await _f.ClientFor(Id.StudentBUserId).GetAsync($"/api/learn/nodes/{lessonId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task Enrolled_student_sees_the_full_tree()
    {
        RequireDocker();
        var (courseId, versionId, _, lessonId) = await CreateDraftCourseAsync("enrol-course");
        await PublishAsync(versionId);

        var student = _f.ClientFor(Id.StudentAUserId);

        var enrol = await student.PostAsync($"/api/enrollments/courses/{courseId}", null);
        enrol.StatusCode.Should().Be(HttpStatusCode.OK, await enrol.Content.ReadAsStringAsync());

        var mine = await student.GetAsync("/api/enrollments/me");
        Data(await Root(mine)).GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var content = await student.GetAsync($"/api/learn/courses/{courseId}/content");
        Data(await Root(content)).GetProperty("AccessLevel").GetString().Should().Be("Full");

        (await student.GetAsync($"/api/learn/nodes/{lessonId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------------- question bank workflow ----------------

    [SkippableFact]
    public async Task Question_review_workflow_moves_pending_to_approved()
    {
        RequireDocker();
        var editor = _f.ClientFor(Id.ContentEditorUserId);

        var bankRes = await editor.PostAsJsonAsync("/api/question-banks", new
        {
            BankName = "Workflow bank",
            SubjectId = await SubjectId(),
            GradeLevelId = await GradeId()
        });
        bankRes.StatusCode.Should().Be(HttpStatusCode.OK, await bankRes.Content.ReadAsStringAsync());
        var bankId = Data(await Root(bankRes)).GetProperty("BankId").GetInt32();

        var createRes = await editor.PostAsJsonAsync("/api/questions", new[]
        {
            new { BankId = bankId, QuestionText = "1 + 1 = ?", QuestionType = (int)QuestionType.FillBlank, CorrectAnswer = "2" }
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.OK, await createRes.Content.ReadAsStringAsync());

        var listRes = await editor.GetAsync($"/api/question-banks/{bankId}/questions");
        var questionId = Data(await Root(listRes)).GetProperty("Items")[0].GetProperty("QuestionId").GetInt32();

        (await editor.PostAsync($"/api/question-banks/questions/{questionId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewRes = await _f.ClientFor(Id.AdminUserId)
            .PostAsJsonAsync($"/api/question-banks/questions/{questionId}/review", new { Approve = true });
        reviewRes.StatusCode.Should().Be(HttpStatusCode.OK, await reviewRes.Content.ReadAsStringAsync());

        var status = await _f.QueryDbAsync(db => db.Questions.AsNoTracking()
            .Where(q => q.QuestionId == questionId).Select(q => q.Status).FirstAsync());
        status.Should().Be(QuestionStatus.Approved);
    }

    [SkippableFact]
    public async Task Students_cannot_reach_the_authoring_endpoints()
    {
        RequireDocker();

        (await _f.ClientFor(Id.StudentAUserId).GetAsync("/api/question-banks"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _f.ClientFor(Id.StudentAUserId).PostAsJsonAsync("/api/courses", new
        {
            SubjectId = await SubjectId(),
            GradeLevelId = await GradeId(),
            Title = "Nope",
            Slug = "nope"
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
