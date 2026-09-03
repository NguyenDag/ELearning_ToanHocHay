using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// Business-logic regression tests for group A2 (P0 remainder + P3 exercise flow).
/// Named after the finding codes in docs/Ra-soat-API-va-ke-hoach-kiem-soat.md.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class A2BusinessLogicTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public A2BusinessLogicTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    // ---------- A2-01: UpdateUser must not wipe password / role / active flag ----------

    [SkippableFact]
    public async Task A2_01_UpdateProfile_keeps_password_role_and_active()
    {
        RequireDocker();

        var before = await _f.QueryDbAsync(db => db.Users.AsNoTracking().SingleAsync(u => u.UserId == Id.StudentAUserId));

        var res = await _f.ClientFor(Id.StudentAUserId)
            .PutAsJsonAsync($"/api/users/{Id.StudentAUserId}", new { FullName = "Renamed Student" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await _f.QueryDbAsync(db => db.Users.AsNoTracking().SingleAsync(u => u.UserId == Id.StudentAUserId));

        after.FullName.Should().Be("Renamed Student");
        after.PasswordHash.Should().Be(before.PasswordHash);
        after.UserType.Should().Be(UserType.Student);
        after.IsActive.Should().BeTrue();
    }

    [SkippableFact]
    public async Task A2_01_UpdateProfileEndpoint_updates_student_school_name()
    {
        RequireDocker();

        var res = await _f.ClientFor(Id.StudentAUserId)
            .PostAsJsonAsync($"/api/users/update-profile/{Id.StudentAUserId}",
                new { FullName = "SchoolKid", SchoolName = "Test High" });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());

        var student = await _f.QueryDbAsync(db =>
            db.Students.AsNoTracking().SingleAsync(s => s.UserId == Id.StudentAUserId));
        student.SchoolName.Should().Be("Test High");
    }

    // ---------- A2-02: server decides the subscription price ----------

    [SkippableFact]
    public async Task A2_02_CreateSubscription_uses_package_price()
    {
        RequireDocker();

        var res = await _f.ClientFor(Id.StudentAUserId)
            .PostAsJsonAsync("/api/subscriptions", new { StudentId = Id.StudentAId, PackageId = Id.PackageId });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());

        var root = await Root(res);
        root.GetProperty("Data").GetProperty("amount").GetDecimal().Should().Be(Id.PackagePrice);
        root.GetProperty("Data").GetProperty("qrUrl").GetString().Should().Contain($"amount={(long)Id.PackagePrice}");

        var subId = root.GetProperty("Data").GetProperty("subscriptionId").GetInt32();
        var (payAmount, subAmount) = await _f.QueryDbAsync(async db =>
        {
            var sub = await db.Subscriptions.AsNoTracking().Include(s => s.Payment).SingleAsync(s => s.SubscriptionId == subId);
            return (sub.Payment!.Amount, sub.AmountPaid);
        });
        payAmount.Should().Be(Id.PackagePrice);
        subAmount.Should().Be(Id.PackagePrice);
    }

    // ---------- A2-03: random exercise can be saved and does not time out ----------

    [SkippableTheory]
    [InlineData(null)]
    [InlineData(15)]
    public async Task A2_03_RandomExercise_saves_and_completes(int? durationMinutes)
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentBUserId);

        var startRes = await client.PostAsJsonAsync("/api/exercise-attempts/start-random", new
        {
            BankId = Id.BankId,
            ExerciseType = 0, // Practice
            NumberOfQuestions = 2,
            MaxScore = 2.0,
            DurationMinutes = durationMinutes
        });
        startRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var start = await Root(startRes);
        var attemptId = start.GetProperty("Data").GetProperty("AttemptId").GetInt32();
        var firstQuestionId = start.GetProperty("Data").GetProperty("Questions")[0].GetProperty("QuestionId").GetInt32();

        var saveRes = await client.PostAsJsonAsync("/api/exercise-attempts/save-answer",
            new { AttemptId = attemptId, QuestionId = firstQuestionId, AnswerText = "42" });
        saveRes.StatusCode.Should().Be(HttpStatusCode.OK, "the attempt must not be treated as expired");

        var completeRes = await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // AttemptStatus serialises as a number: 1 = Submitted, 2 = Timeout.
        var status = (await Root(completeRes)).GetProperty("Data").GetProperty("Status").GetInt32();
        status.Should().Be(1, "there is no fake timeout anymore");
    }

    // ---------- A2-04: complete returns immediately; AI feedback is queued ----------

    [SkippableFact]
    public async Task A2_04_Complete_returns_fast_and_queues_feedback()
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentBUserId);

        var attemptId = await StartExercise(client, Id.ExerciseId);
        // Answer MC wrong so a feedback job is queued.
        await SaveAnswer(client, attemptId, Id.McQuestionId, selectedOptionId: null, answerText: "definitely wrong");

        var sw = Stopwatch.StartNew();
        var completeRes = await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });
        sw.Stop();

        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10), "AI feedback must not block the response");

        var data = (await Root(completeRes)).GetProperty("Data");
        foreach (var d in data.GetProperty("AnswerDetails").EnumerateArray())
            d.TryGetProperty("FullSolution", out var fs).Should().BeFalse("AI fields fill in later via /result");

        var statusRes = await client.GetAsync($"/api/exercise-attempts/{attemptId}/feedback-status");
        statusRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Root(statusRes)).GetProperty("Data").GetProperty("TotalWrong").GetInt32().Should().BeGreaterThan(0);
    }

    // ---------- A2-07: /submit and /submit-answer are gone ----------

    [SkippableFact]
    public async Task A2_07_Removed_submit_endpoints_return_404()
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentBUserId);

        (await client.PostAsJsonAsync("/api/exercise-attempts/submit",
            new { AttemptId = Id.AttemptAId, Answers = Array.Empty<object>() })).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        (await client.PostAsJsonAsync("/api/exercise-attempts/submit-answer",
            new { AttemptId = Id.AttemptAId, QuestionId = Id.McQuestionId })).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- A2-08: MaxAttempts is enforced ----------

    [SkippableFact]
    public async Task A2_08_MaxAttempts_of_one_blocks_second_attempt()
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentBUserId);

        var attemptId = await StartExercise(client, Id.MaxAttemptsExerciseId);
        (await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/exercise-attempts/start",
            new { ExerciseId = Id.MaxAttemptsExerciseId });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await second.Content.ReadAsStringAsync()).ToLower().Should().Contain("attempt");
    }

    // ---------- A2-09: grading matrix end-to-end ----------

    [SkippableFact]
    public async Task A2_09_Grades_every_question_type()
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentBUserId);

        var attemptId = await StartExercise(client, Id.ExerciseId);
        await SaveAnswer(client, attemptId, Id.McQuestionId, Id.McCorrectOptionId, null);
        await SaveAnswer(client, attemptId, Id.TfQuestionId, Id.TfTrueOptionId, null);
        await SaveAnswer(client, attemptId, Id.FillBlankQuestionId, null, "0.5"); // "1/2" accepted
        await SaveAnswer(client, attemptId, Id.EssayQuestionId, null, "Zero is even because it is divisible by 2.");

        var res = await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = (await Root(res)).GetProperty("Data");
        data.GetProperty("TotalScore").GetDouble().Should().Be(3);
        data.GetProperty("CorrectAnswers").GetInt32().Should().Be(3);
        data.GetProperty("WrongAnswers").GetInt32().Should().Be(0, "the essay is pending, not wrong");
        data.GetProperty("HasPendingManualGrading").GetBoolean().Should().BeTrue();

        var essay = data.GetProperty("AnswerDetails").EnumerateArray()
            .Single(d => d.GetProperty("QuestionId").GetInt32() == Id.EssayQuestionId);
        essay.GetProperty("NeedsManualGrading").GetBoolean().Should().BeTrue();
    }

    // ---------- helpers ----------

    private async Task<int> StartExercise(HttpClient client, int exerciseId)
    {
        var res = await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exerciseId });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await Root(res)).GetProperty("Data").GetProperty("AttemptId").GetInt32();
    }

    private async Task SaveAnswer(HttpClient client, int attemptId, int questionId, int? selectedOptionId, string? answerText)
    {
        var res = await client.PostAsJsonAsync("/api/exercise-attempts/save-answer",
            new { AttemptId = attemptId, QuestionId = questionId, SelectedOptionId = selectedOptionId, AnswerText = answerText });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
