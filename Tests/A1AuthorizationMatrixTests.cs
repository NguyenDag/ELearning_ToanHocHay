using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// Authorization-matrix tests for group A1 (the P0 Definition of Done).
/// Each test is named after the finding code in docs/Ra-soat-API-va-ke-hoach-kiem-soat.md.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class A1AuthorizationMatrixTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public A1AuthorizationMatrixTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    // ---------- A1-01: UserController ----------

    [SkippableFact]
    public async Task A1_01_AnonymousCannotListUsers()
    {
        RequireDocker();
        var res = await _f.ClientFor(null).GetAsync("/api/users");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task A1_01_StudentCannotListUsers()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId).GetAsync("/api/users");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_01_StudentCannotDeleteAnotherUser()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId).DeleteAsync($"/api/users/{Id.StudentBUserId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_01_AdminCanListUsers()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.AdminUserId).GetAsync("/api/users");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- A1-02: ExerciseAttempts ownership ----------

    [SkippableFact]
    public async Task A1_02_AnonymousCannotSaveAnswer()
    {
        RequireDocker();
        var res = await _f.ClientFor(null)
            .PostAsJsonAsync("/api/exercise-attempts/save-answer", new { AttemptId = Id.AttemptAId, QuestionId = 1 });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task A1_02_OtherStudentCannotSaveAnswerOnMyAttempt()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentBUserId)
            .PostAsJsonAsync("/api/exercise-attempts/save-answer", new { AttemptId = Id.AttemptAId, QuestionId = 1 });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_02_OtherStudentCannotViewMyResult()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentBUserId)
            .GetAsync($"/api/exercise-attempts/{Id.AttemptAId}/result");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_02_OtherStudentCannotReadMyHistory()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentBUserId)
            .GetAsync($"/api/exercise-attempts/student/{Id.StudentAId}/history");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_02_OwnerAndLinkedParentCanReadHistory()
    {
        RequireDocker();
        var owner = await _f.ClientFor(Id.StudentAUserId)
            .GetAsync($"/api/exercise-attempts/student/{Id.StudentAId}/history");
        owner.StatusCode.Should().Be(HttpStatusCode.OK);

        var linkedParent = await _f.ClientFor(Id.ParentLinkedUserId)
            .GetAsync($"/api/exercise-attempts/student/{Id.StudentAId}/history");
        linkedParent.StatusCode.Should().Be(HttpStatusCode.OK);

        var strangerParent = await _f.ClientFor(Id.ParentUnlinkedUserId)
            .GetAsync($"/api/exercise-attempts/student/{Id.StudentAId}/history");
        strangerParent.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- A1-03: Subscription / Payment / Package ----------

    [SkippableFact]
    public async Task A1_03_AnonymousCannotPatchSubscriptionStatus()
    {
        RequireDocker();
        var res = await _f.ClientFor(null)
            .PatchAsJsonAsync($"/api/subscriptions/{Id.SubscriptionAId}/status", new { Status = "Active" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task A1_03_StudentCannotPatchSubscriptionStatus()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId)
            .PatchAsJsonAsync($"/api/subscriptions/{Id.SubscriptionAId}/status", new { Status = "Active" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_03_AnonymousCannotMarkPaymentCompleted()
    {
        RequireDocker();
        var res = await _f.ClientFor(null)
            .PutAsJsonAsync($"/api/payments/update-status/{Id.PaymentAId}", new { Status = "Completed" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task A1_03_StudentCannotCreatePackage()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId)
            .PostAsJsonAsync("/api/packages", new { PackageName = "x", Price = 1, DurationDays = 30 });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_03_FinanceCanListPayments()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.FinanceUserId).GetAsync("/api/payments");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task A1_03_PackageListIsPublic()
    {
        RequireDocker();
        var res = await _f.ClientFor(null).GetAsync("/api/packages");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- A1-04: Content endpoints ----------

    [SkippableFact]
    public async Task A1_04_StudentCannotCreateExercise()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId)
            .PostAsJsonAsync("/api/exercises", new { ExerciseName = "x" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_04_StudentCannotBulkCreateQuestions()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId)
            .PostAsJsonAsync("/api/questions", new[] { new { QuestionText = "x" } });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task A1_04_ContentEditorPassesAuthorization()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.ContentEditorUserId)
            .PostAsJsonAsync("/api/exercises", new { ExerciseName = "New exercise" });
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ---------- A1-05: DashboardController ----------

    [SkippableFact]
    public async Task A1_05_OtherStudentCannotReadDashboard()
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentBUserId);

        foreach (var path in new[] { "overview", "chapter-score-comparison", "ai-assessment", "ai-roadmap" })
        {
            var res = await client.GetAsync($"/api/students/{Id.StudentAId}/dashboard/{path}");
            res.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"endpoint {path} must block another student");
        }
    }

    // ---------- A1-06: AI endpoints ----------

    [SkippableFact]
    public async Task A1_06_AnonymousCannotCallAi()
    {
        RequireDocker();
        var anon = _f.ClientFor(null);

        (await anon.PostAsJsonAsync("/api/ai-hints", new { AttemptId = Id.AttemptAId, QuestionId = 1 }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anon.PostAsJsonAsync("/api/ai-feedback", new { AttemptId = Id.AttemptAId, QuestionId = 1 }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anon.PostAsJsonAsync("/api/chatbot/message", new { text = "hi" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task A1_06_OtherStudentCannotReadMyHints()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentBUserId)
            .GetAsync($"/api/ai-hints/by-attempt/{Id.AttemptAId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- A1-11: /api/auth/me ----------

    [SkippableFact]
    public async Task A1_11_MeReturnsEmailAndUserType()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId).GetAsync("/api/auth/me");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("Data");
        data.GetProperty("Email").GetString().Should().Be("student.a@test.local");
        data.GetProperty("UserType").GetString().Should().Be("Student");
    }
}
