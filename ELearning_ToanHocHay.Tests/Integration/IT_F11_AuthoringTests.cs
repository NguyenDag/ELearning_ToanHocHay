using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F11 — Soạn nội dung / authoring (IT-F11).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Authoring")]
public class IT_F11_AuthoringTests : IntegrationTest
{
    public IT_F11_AuthoringTests(ApiFactory app) : base(app) { }

    private static string Rand() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Môn học mới (combo Subject/Grade/Framework luôn duy nhất cho <c>POST /api/courses</c>).</summary>
    private Task<int> NewSubjectId() => App.Db(async db =>
    {
        var s = new Subject
        {
            Code = $"S{Rand()}"[..8], Name = $"Môn {Rand()}",
            Slug = $"mon-{Rand()}", ColorHex = "#123456", DisplayOrder = 50, IsActive = true,
        };
        db.Subjects.Add(s);
        await db.SaveChangesAsync();
        return s.SubjectId;
    });

    /// <summary>Editor tạo 1 khoá mới → trả <c>(courseId, versionId)</c> của Draft v1.</summary>
    private async Task<(int courseId, int versionId)> CreateDraftCourseAsync(HttpClient editor)
    {
        var subjectId = await NewSubjectId();
        var res = await editor.PostAsJsonAsync("/api/courses", new
        {
            SubjectId = subjectId, GradeLevelId = 1, Title = $"Khoá {Rand()}", Slug = $"khoa-{Rand()}",
            ListPrice = 0m, IsPurchasable = true, DisplayOrder = 0,
        });
        await res.ShouldBeOk();
        var data = await res.DataAsync();
        var v = data.GetProperty("Versions").EnumerateArray().First();
        return (data.GetProperty("CourseId").GetInt32(), v.GetProperty("CourseVersionId").GetInt32());
    }

    private async Task<int> CreateNodeAsync(
        HttpClient editor, int versionId, string nodeType, int? parentNodeId, string? title = null)
    {
        var res = await editor.PostAsJsonAsync($"/api/content/versions/{versionId}/nodes", new
        {
            ParentNodeId = parentNodeId, NodeType = nodeType, Title = title ?? $"{nodeType} {Rand()}",
        });
        await res.ShouldBeOk();
        return (await res.DataAsync()).GetProperty("NodeId").GetInt32();
    }

    // ---------------------------------------------------------------- IT-F11-01

    [SkippableFact] // IT-F11-01
    public async Task IT_F11_01_Catalog_writes_need_a_content_role()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var student = App.AsRole(TestRole.StudentA);

        var grade = new { Code = $"G{Rand()}"[..8], Name = $"Lớp {Rand()}", DisplayOrder = 90, IsActive = true };
        (await student.PostAsJsonAsync("/api/catalog/grade-levels", grade))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await (await editor.PostAsJsonAsync("/api/catalog/grade-levels", grade)).ShouldBeOk();

        var framework = new { Code = $"F{Rand()}"[..8], Name = $"Khung {Rand()}", Publisher = "NXB Test", IsActive = true };
        (await student.PostAsJsonAsync("/api/catalog/frameworks", framework))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await (await editor.PostAsJsonAsync("/api/catalog/frameworks", framework)).ShouldBeOk();
    }

    // ---------------------------------------------------------------- IT-F11-02

    [SkippableFact] // IT-F11-02
    public async Task IT_F11_02_Version_lifecycle_draft_to_published()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var admin = App.AsRole(TestRole.Admin);

        var (courseId, versionId) = await CreateDraftCourseAsync(editor);
        await CreateNodeAsync(editor, versionId, "Chapter", null);

        var submit = await editor.PostAsync($"/api/courses/versions/{versionId}/submit", null);
        await submit.ShouldBeOk();
        (await submit.DataAsync()).GetProperty("State").GetString().Should().Be("InReview");

        var review = await admin.PostAsJsonAsync($"/api/courses/versions/{versionId}/review", new { Decision = "Approve" });
        await review.ShouldBeOk();
        (await review.DataAsync()).GetProperty("State").GetString().Should().Be("Approved");

        var publish = await admin.PostAsync($"/api/courses/versions/{versionId}/publish", null);
        await publish.ShouldBeOk();

        await App.Db(async db =>
        {
            (await db.CourseVersions.SingleAsync(v => v.CourseVersionId == versionId))
                .State.Should().Be(VersionState.Published);
            (await db.Courses.SingleAsync(c => c.CourseId == courseId))
                .Status.Should().Be(CourseStatus.Published);
        });
    }

    // ---------------------------------------------------------------- IT-F11-03

    [SkippableFact] // IT-F11-03
    public async Task IT_F11_03_Rejected_version_cannot_be_published()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var admin = App.AsRole(TestRole.Admin);

        var (_, versionId) = await CreateDraftCourseAsync(editor);
        await CreateNodeAsync(editor, versionId, "Chapter", null);
        await (await editor.PostAsync($"/api/courses/versions/{versionId}/submit", null)).ShouldBeOk();

        var review = await admin.PostAsJsonAsync($"/api/courses/versions/{versionId}/review", new { Decision = "Reject" });
        await review.ShouldBeOk();

        await App.Db(async db =>
            (await db.CourseVersions.SingleAsync(v => v.CourseVersionId == versionId))
                .State.Should().BeOneOf(VersionState.Draft, VersionState.Archived));

        var publish = await admin.PostAsync($"/api/courses/versions/{versionId}/publish", null);
        await publish.ShouldBeError(HttpStatusCode.BadRequest, "Approved");
    }

    // ---------------------------------------------------------------- IT-F11-04

    [SkippableFact] // IT-F11-04
    public async Task IT_F11_04_Content_cannot_be_edited_after_publish()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var c = await Flow.PublishCourseAsync();

        var res = await editor.PostAsJsonAsync($"/api/content/versions/{c.VersionId}/nodes",
            new { NodeType = "Chapter", Title = "Muộn rồi" });
        await res.ShouldBeError(HttpStatusCode.BadRequest, "Draft");
    }

    // ---------------------------------------------------------------- IT-F11-05

    [SkippableFact] // IT-F11-05
    public async Task IT_F11_05_A_lesson_cannot_be_created_directly_under_root()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var (_, versionId) = await CreateDraftCourseAsync(editor);

        var res = await editor.PostAsJsonAsync($"/api/content/versions/{versionId}/nodes",
            new { NodeType = "Lesson", Title = "Bài lạc chỗ" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------------- IT-F11-06

    [SkippableFact] // IT-F11-06
    public async Task IT_F11_06_Updating_a_node_records_a_revision_and_can_restore()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var (_, versionId) = await CreateDraftCourseAsync(editor);
        var nodeId = await CreateNodeAsync(editor, versionId, "Chapter", null, "Tiêu đề gốc");

        await (await editor.PutAsJsonAsync($"/api/content/nodes/{nodeId}", new { Title = "Tiêu đề mới" })).ShouldBeOk();

        var revisions = await editor.GetAsync($"/api/content/nodes/{nodeId}/revisions");
        await revisions.ShouldBeOk();
        (await revisions.DataAsync()).GetArrayLength().Should().BeGreaterThan(0);

        await (await editor.PostAsync($"/api/content/nodes/{nodeId}/revisions/1/restore", null)).ShouldBeOk();

        await App.Db(async db =>
            (await db.ContentNodes.SingleAsync(n => n.NodeId == nodeId)).Title.Should().Be("Tiêu đề gốc"));
    }

    // ---------------------------------------------------------------- IT-F11-07

    [SkippableFact] // IT-F11-07
    public async Task IT_F11_07_Moving_a_node_rewrites_its_subtree_path()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var (_, versionId) = await CreateDraftCourseAsync(editor);

        var chapterA = await CreateNodeAsync(editor, versionId, "Chapter", null);
        var topic = await CreateNodeAsync(editor, versionId, "Topic", chapterA);
        var lesson = await CreateNodeAsync(editor, versionId, "Lesson", topic);
        var chapterB = await CreateNodeAsync(editor, versionId, "Chapter", null);

        var move = await editor.PatchAsJsonAsync($"/api/content/nodes/{topic}/move", new { NewParentNodeId = chapterB });
        await move.ShouldBeOk();

        await App.Db(async db =>
        {
            var b = await db.ContentNodes.SingleAsync(n => n.NodeId == chapterB);
            var t = await db.ContentNodes.SingleAsync(n => n.NodeId == topic);
            var l = await db.ContentNodes.SingleAsync(n => n.NodeId == lesson);

            t.ParentNodeId.Should().Be(chapterB);
            t.MaterializedPath.Should().StartWith(b.MaterializedPath);
            l.MaterializedPath.Should().StartWith(b.MaterializedPath);
            l.MaterializedPath.Should().Contain($"/{topic}/");
        });
    }

    // ---------------------------------------------------------------- IT-F11-08

    [SkippableFact] // IT-F11-08
    public async Task IT_F11_08_Reordering_siblings_updates_the_order()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var (_, versionId) = await CreateDraftCourseAsync(editor);

        var chapter = await CreateNodeAsync(editor, versionId, "Chapter", null);
        var lesson1 = await CreateNodeAsync(editor, versionId, "Lesson", chapter);
        var lesson2 = await CreateNodeAsync(editor, versionId, "Lesson", chapter);

        var res = await editor.PostAsJsonAsync(
            $"/api/content/versions/{versionId}/nodes/reorder?parentNodeId={chapter}",
            new { OrderedNodeIds = new[] { lesson2, lesson1 } });
        await res.ShouldBeOk();

        await App.Db(async db =>
        {
            var l1 = await db.ContentNodes.SingleAsync(n => n.NodeId == lesson1);
            var l2 = await db.ContentNodes.SingleAsync(n => n.NodeId == lesson2);
            l2.OrderIndex.Should().BeLessThan(l1.OrderIndex);
        });
    }

    // ---------------------------------------------------------------- IT-F11-09

    [SkippableFact] // IT-F11-09
    public async Task IT_F11_09_Review_can_attach_comments_and_editor_resolves_them()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var admin = App.AsRole(TestRole.Admin);

        var (_, versionId) = await CreateDraftCourseAsync(editor);
        var nodeId = await CreateNodeAsync(editor, versionId, "Chapter", null);
        await (await editor.PostAsync($"/api/courses/versions/{versionId}/submit", null)).ShouldBeOk();

        var review = await admin.PostAsJsonAsync($"/api/courses/versions/{versionId}/review", new
        {
            Decision = "RequestChanges",
            Comments = new[] { new { NodeId = nodeId, Body = "Sửa lại tiêu đề chương" } },
        });
        await review.ShouldBeOk();

        var reviews = await editor.GetAsync($"/api/courses/versions/{versionId}/reviews");
        await reviews.ShouldBeOk();
        var commentId = (await reviews.DataAsync()).EnumerateArray()
            .SelectMany(r => r.GetProperty("Comments").EnumerateArray())
            .Select(c => c.GetProperty("CommentId").GetInt32())
            .First();

        await (await editor.PostAsync($"/api/courses/reviews/comments/{commentId}/resolve", null)).ShouldBeOk();

        await App.Db(async db =>
            (await db.ReviewComments.SingleAsync(c => c.CommentId == commentId))
                .Status.Should().Be(CommentStatus.Resolved));
    }

    // ---------------------------------------------------------------- IT-F11-10

    [SkippableFact] // IT-F11-10
    public async Task IT_F11_10_Question_review_workflow_and_reviewer_role_gate()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var admin = App.AsRole(TestRole.Admin);

        var create = await editor.PostAsJsonAsync("/api/questions", new[]
        {
            new { BankId = Ids.BankId, QuestionText = $"7 + 5 = ? ({Rand()})", QuestionType = "FillBlank", CorrectAnswer = "12" },
        });
        await create.ShouldBeOk();
        var questionId = (await create.DataAsync()).EnumerateArray().First().GetProperty("QuestionId").GetInt32();

        await (await editor.PostAsync($"/api/question-banks/questions/{questionId}/submit", null)).ShouldBeOk();

        // Editor is a content role but not a reviewer — the review step is reviewer/admin only.
        (await editor.PostAsJsonAsync($"/api/question-banks/questions/{questionId}/review", new { Approve = true }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await (await admin.PostAsJsonAsync($"/api/question-banks/questions/{questionId}/review", new { Approve = true }))
            .ShouldBeOk();

        await App.Db(async db =>
            (await db.Questions.SingleAsync(q => q.QuestionId == questionId))
                .Status.Should().Be(QuestionStatus.Approved));
    }

    // ---------------------------------------------------------------- IT-F11-11

    [SkippableFact] // IT-F11-11
    public async Task IT_F11_11_Block_crud_on_a_draft_node_is_content_role_only()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var (_, versionId) = await CreateDraftCourseAsync(editor);
        var nodeId = await CreateNodeAsync(editor, versionId, "Chapter", null);

        var add = await editor.PostAsJsonAsync($"/api/content/nodes/{nodeId}/blocks",
            new { BlockType = "Text", ContentText = "Đoạn văn" });
        await add.ShouldBeOk();
        var blockId = (await add.DataAsync()).GetProperty("BlockId").GetInt32();

        await (await editor.PutAsJsonAsync($"/api/content/blocks/{blockId}",
            new { BlockType = "Text", ContentText = "Đoạn văn (đã sửa)" })).ShouldBeOk();

        (await App.AsRole(TestRole.StudentA).PostAsJsonAsync($"/api/content/nodes/{nodeId}/blocks",
            new { BlockType = "Text", ContentText = "x" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await (await editor.DeleteAsync($"/api/content/blocks/{blockId}")).ShouldBeOk();

        await App.Db(async db =>
            (await db.Set<ContentBlock>().AnyAsync(b => b.BlockId == blockId)).Should().BeFalse());
    }

    // ---------------------------------------------------------------- IT-F11-12

    [SkippableFact] // IT-F11-12
    public async Task IT_F11_12_Exercise_publish_unpublish_workflow()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);

        var create = await editor.PostAsJsonAsync("/api/exercises", new
        {
            ExerciseName = $"Bài KT {Rand()}", ExerciseType = "Quiz",
            TotalQuestions = 1, TotalScores = 1.0, PassingScore = 1.0, Status = "Draft",
        });
        await create.ShouldBeOk();
        var exerciseId = (await create.DataAsync()).GetProperty("ExerciseId").GetInt32();

        await (await editor.PostAsJsonAsync($"/api/exercises/{exerciseId}/questions",
            new { QuestionIds = new[] { Ids.McQuestionId }, ScorePerQuestion = 1.0 })).ShouldBeOk();

        await (await editor.PostAsync($"/api/exercises/{exerciseId}/publish", null)).ShouldBeOk();
        await App.Db(async db =>
            (await db.Exercises.SingleAsync(e => e.ExerciseId == exerciseId)).Status.Should().Be(ExerciseStatus.Published));

        await (await editor.PostAsync($"/api/exercises/{exerciseId}/unpublish", null)).ShouldBeOk();
        await App.Db(async db =>
            (await db.Exercises.SingleAsync(e => e.ExerciseId == exerciseId)).Status.Should().Be(ExerciseStatus.Draft));

        (await App.AsRole(TestRole.StudentA).PostAsJsonAsync("/api/exercises", new
        {
            ExerciseName = "x", ExerciseType = "Quiz", TotalQuestions = 1, TotalScores = 1.0, PassingScore = 1.0,
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------- IT-F11-13

    [SkippableFact] // IT-F11-13
    public async Task IT_F11_13_ContentEditor_passes_authorization_on_exercise_create()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.Editor).PostAsJsonAsync("/api/exercises", new
        {
            ExerciseName = $"Auth {Rand()}", ExerciseType = "Quiz",
            TotalQuestions = 1, TotalScores = 1.0, PassingScore = 1.0, Status = "Draft",
        });

        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        await res.ShouldBeOk();
    }
}
