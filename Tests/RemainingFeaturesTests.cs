using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// Remaining items across P2 (node revisions / move / review comments),
/// P5 (refund), P6 (chat escalation) and P6/P7 (SystemConfig).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class RemainingFeaturesTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public RemainingFeaturesTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    private static JsonElement Data(JsonElement root) => root.GetProperty("Data");

    private async Task<int> SubjectId() => await _f.QueryDbAsync(db => db.Subjects
        .Where(s => s.Code == "MATH").Select(s => s.SubjectId).FirstAsync());
    private async Task<int> GradeId() => await _f.QueryDbAsync(db => db.GradeLevels
        .Where(g => g.Code == "G6").Select(g => g.GradeLevelId).FirstAsync());

    private async Task<int> NewDraftVersionAsync(string slug)
    {
        var editor = _f.ClientFor(Id.ContentEditorUserId);
        var fw = await editor.PostAsJsonAsync("/api/catalog/frameworks", new { Code = $"RF-{slug}", Name = slug });
        var fwId = Data(await Root(fw)).GetProperty("FrameworkId").GetInt32();

        var course = await editor.PostAsJsonAsync("/api/courses", new
        {
            SubjectId = await SubjectId(), GradeLevelId = await GradeId(), FrameworkId = fwId,
            Title = $"RF {slug}", Slug = $"rf-{slug}"
        });
        course.StatusCode.Should().Be(HttpStatusCode.OK, await course.Content.ReadAsStringAsync());
        return Data(await Root(course)).GetProperty("Versions")[0].GetProperty("CourseVersionId").GetInt32();
    }

    private async Task<int> AddNodeAsync(int versionId, int nodeType, string title, int? parentId = null)
    {
        var res = await _f.ClientFor(Id.ContentEditorUserId).PostAsJsonAsync(
            $"/api/content/versions/{versionId}/nodes",
            new { ParentNodeId = parentId, NodeType = nodeType, Title = title });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        return Data(await Root(res)).GetProperty("NodeId").GetInt32();
    }

    // ---------------- P2: node revisions ----------------
    [SkippableFact]
    public async Task Updating_a_node_records_a_revision_that_can_be_restored()
    {
        RequireDocker();
        var editor = _f.ClientFor(Id.ContentEditorUserId);
        var versionId = await NewDraftVersionAsync("rev");
        var chapterId = await AddNodeAsync(versionId, (int)NodeType.Chapter, "Original title");

        (await editor.PutAsJsonAsync($"/api/content/nodes/{chapterId}", new { Title = "Renamed" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var revs = Data(await Root(await editor.GetAsync($"/api/content/nodes/{chapterId}/revisions")));
        revs.GetArrayLength().Should().Be(1);
        revs[0].GetProperty("RevisionNumber").GetInt32().Should().Be(1);

        (await editor.PostAsync($"/api/content/nodes/{chapterId}/revisions/1/restore", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var title = await _f.QueryDbAsync(db => db.ContentNodes.AsNoTracking()
            .Where(n => n.NodeId == chapterId).Select(n => n.Title).FirstAsync());
        title.Should().Be("Original title");
    }

    // ---------------- P2: re-parent ----------------
    [SkippableFact]
    public async Task Moving_a_node_rewrites_its_subtree_path()
    {
        RequireDocker();
        var editor = _f.ClientFor(Id.ContentEditorUserId);
        var versionId = await NewDraftVersionAsync("move");
        var chapterId = await AddNodeAsync(versionId, (int)NodeType.Chapter, "Ch");
        var topicId = await AddNodeAsync(versionId, (int)NodeType.Topic, "Topic", chapterId);
        var lessonId = await AddNodeAsync(versionId, (int)NodeType.Lesson, "Lesson", chapterId);

        var move = await editor.PatchAsJsonAsync($"/api/content/nodes/{lessonId}/move", new { NewParentNodeId = topicId });
        move.StatusCode.Should().Be(HttpStatusCode.OK, await move.Content.ReadAsStringAsync());

        var (parent, path, depth) = await _f.QueryDbAsync(db => db.ContentNodes.AsNoTracking()
            .Where(n => n.NodeId == lessonId)
            .Select(n => new ValueTuple<int?, string, int>(n.ParentNodeId, n.MaterializedPath, n.Depth)).FirstAsync());
        parent.Should().Be(topicId);
        path.Should().Be($"/{chapterId}/{topicId}/{lessonId}/");
        depth.Should().Be(2);
    }

    // ---------------- P2: review comments ----------------
    [SkippableFact]
    public async Task Review_can_attach_comments_that_editors_resolve()
    {
        RequireDocker();
        var editor = _f.ClientFor(Id.ContentEditorUserId);
        var admin = _f.ClientFor(Id.AdminUserId);
        var versionId = await NewDraftVersionAsync("comments");
        var chapterId = await AddNodeAsync(versionId, (int)NodeType.Chapter, "Ch");

        (await editor.PostAsync($"/api/courses/versions/{versionId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var review = await admin.PostAsJsonAsync($"/api/courses/versions/{versionId}/review", new
        {
            Decision = (int)ReviewDecision.RequestChanges,
            Summary = "needs work",
            Comments = new[] { new { NodeId = chapterId, Body = "Rename this chapter" } }
        });
        review.StatusCode.Should().Be(HttpStatusCode.OK, await review.Content.ReadAsStringAsync());

        var reviews = Data(await Root(await editor.GetAsync($"/api/courses/versions/{versionId}/reviews")));
        var comment = reviews[0].GetProperty("Comments")[0];
        comment.GetProperty("NodeId").GetInt32().Should().Be(chapterId);
        var commentId = comment.GetProperty("CommentId").GetInt32();

        (await editor.PostAsync($"/api/courses/reviews/comments/{commentId}/resolve", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await _f.QueryDbAsync(db => db.ReviewComments.AsNoTracking()
            .Where(c => c.CommentId == commentId).Select(c => c.Status).FirstAsync());
        status.Should().Be(CommentStatus.Resolved);
    }

    // ---------------- P5: refund ----------------
    [SkippableFact]
    public async Task Finance_can_refund_a_completed_payment_and_it_cancels_the_subscription()
    {
        RequireDocker();

        var (paymentId, subId) = await _f.QueryDbAsync(async db =>
        {
            var pkg = await db.Packages.FirstAsync();
            var p = new Payment
            {
                PaidByUserId = Id.StudentBUserId, StudentId = Id.StudentBId, Amount = pkg.Price,
                Status = PaymentStatus.Completed, PaymentMethod = PaymentMethod.BankTransfer
            };
            db.Payments.Add(p);
            await db.SaveChangesAsync();
            var s = new Subscription
            {
                StudentId = Id.StudentBId, PackageId = pkg.PackageId, PaymentId = p.PaymentId,
                Status = SubscriptionStatus.Active, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
                AmountPaid = pkg.Price
            };
            db.Subscriptions.Add(s);
            await db.SaveChangesAsync();
            return (p.PaymentId, s.SubscriptionId);
        });

        var res = await _f.ClientFor(Id.FinanceUserId)
            .PostAsJsonAsync($"/api/payments/{paymentId}/refund", new { Reason = "customer request" });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());

        var state = await _f.QueryDbAsync(db => db.Payments.AsNoTracking()
            .Where(p => p.PaymentId == paymentId)
            .Select(p => new { p.Status, p.RefundAmount }).FirstAsync());
        state.Status.Should().Be(PaymentStatus.Refunded);
        state.RefundAmount.Should().BeGreaterThan(0);

        var subStatus = await _f.QueryDbAsync(db => db.Subscriptions.AsNoTracking()
            .Where(s => s.SubscriptionId == subId).Select(s => s.Status).FirstAsync());
        subStatus.Should().Be(SubscriptionStatus.Cancelled);
    }

    // ---------------- P6: chat escalation ----------------
    [SkippableFact]
    public async Task Chat_escalation_moves_the_conversation_to_a_support_agent()
    {
        RequireDocker();

        var staffUserId = await _f.QueryDbAsync(async db =>
        {
            var u = new User
            {
                Email = $"staff-{Guid.NewGuid():N}@test.local", PasswordHash = "x", FullName = "Support",
                UserType = UserType.SupportStaff, IsEmailConfirmed = true, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            return u.UserId;
        });

        var student = _f.ClientFor(Id.StudentAUserId);
        var req = await student.PostAsync("/api/chatbot/request-human", null);
        req.StatusCode.Should().Be(HttpStatusCode.OK, await req.Content.ReadAsStringAsync());
        var conversationId = Data(await Root(req)).GetProperty("ConversationId").GetInt32();

        var staff = _f.ClientFor(staffUserId);
        var queue = Data(await Root(await staff.GetAsync("/api/chatbot/staff/queue")));
        queue.EnumerateArray().Select(c => c.GetProperty("ConversationId").GetInt32())
            .Should().Contain(conversationId);

        (await staff.PostAsync($"/api/chatbot/staff/conversations/{conversationId}/assign", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await staff.PostAsJsonAsync($"/api/chatbot/staff/conversations/{conversationId}/reply", new { Text = "Chào em" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var conv = await _f.QueryDbAsync(db => db.ChatConversations.AsNoTracking()
            .Where(c => c.ConversationId == conversationId)
            .Select(c => new { c.Status, c.AssignedStaffId }).FirstAsync());
        conv.Status.Should().Be(ChatStatus.WithAgent);
        conv.AssignedStaffId.Should().Be(staffUserId);
    }

    // ---------------- P6/P7: SystemConfig ----------------
    [SkippableFact]
    public async Task Admin_can_read_and_update_system_config()
    {
        RequireDocker();
        var admin = _f.ClientFor(Id.AdminUserId);

        (await _f.ClientFor(Id.StudentAUserId).GetAsync("/api/admin/config"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var set = await admin.PutAsJsonAsync("/api/admin/config/notify.inactivity.days", new { Value = "7" });
        set.StatusCode.Should().Be(HttpStatusCode.OK, await set.Content.ReadAsStringAsync());

        var all = Data(await Root(await admin.GetAsync("/api/admin/config?group=notify")));
        all.EnumerateArray()
            .First(c => c.GetProperty("ConfigKey").GetString() == "notify.inactivity.days")
            .GetProperty("ConfigValue").GetString().Should().Be("7");

        await admin.PutAsJsonAsync("/api/admin/config/notify.inactivity.days", new { Value = "3" }); // reset
    }
}
