using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F4 — Làm bài tập / kiểm tra (IT-F4).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Attempt")]
public class IT_F4_ExerciseAttemptTests : IntegrationTest
{
    public IT_F4_ExerciseAttemptTests(ApiFactory app) : base(app) { }

    private static async Task<int> AttemptIdOf(HttpResponseMessage res)
        => (await res.DataAsync()).GetProperty("AttemptId").GetInt32();

    private async Task<(HttpClient client, int attemptId)> StartFreshAttempt(int? maxAttempts = null)
    {
        var (userId, _) = await Flow.NewStudentAsync();
        var exId = await Flow.PublishExerciseAsync(maxAttempts: maxAttempts);
        var client = App.As(userId);
        var start = await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId });
        await start.ShouldBeOk();
        return (client, await AttemptIdOf(start));
    }

    private Task<HttpResponseMessage> Save(HttpClient client, int attemptId, int questionId, int? optionId = null, string? text = null)
        => client.PostAsJsonAsync("/api/exercise-attempts/save-answer", new
        {
            AttemptId = attemptId, QuestionId = questionId, SelectedOptionId = optionId, AnswerText = text,
        });

    [SkippableFact] // IT-F4-01
    public async Task IT_F4_01_Start_creates_an_in_progress_attempt_with_planned_end()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        var exId = await Flow.PublishExerciseAsync(durationMinutes: 30);

        var start = await App.As(userId).PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId });
        await start.ShouldBeOk();

        await App.Db(async db =>
        {
            var a = await db.ExerciseAttempts.SingleAsync(x => x.StudentId == studentId && x.ExerciseId == exId);
            a.Status.Should().Be(AttemptStatus.InProgress);
            a.PlannedEndTime.Should().NotBeNull();
        });
    }

    [SkippableFact] // IT-F4-02
    public async Task IT_F4_02_Premium_exercise_on_free_tier_is_403()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var exId = await Flow.PublishExerciseAsync(tier: AccessTier.Premium);

        var res = await App.As(userId).PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F4-05
    public async Task IT_F4_05_Save_answer_twice_overwrites_the_row()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt();

        await (await Save(client, attemptId, Ids.FillBlankQuestionId, text: "0.4")).ShouldBeOk();
        await (await Save(client, attemptId, Ids.FillBlankQuestionId, text: "0.5")).ShouldBeOk();

        await App.Db(async db =>
        {
            var rows = await db.StudentAnswers.Where(a => a.AttemptId == attemptId && a.QuestionId == Ids.FillBlankQuestionId).ToListAsync();
            rows.Should().ContainSingle();
            rows[0].AnswerText.Should().Be("0.5");
        });
    }

    [SkippableFact] // IT-F4-06
    public async Task IT_F4_06_Save_answer_on_another_students_attempt_is_403()
    {
        RequireDocker();
        var (_, attemptId) = await StartFreshAttempt();
        var (otherUserId, _) = await Flow.NewStudentAsync();

        var res = await Save(App.As(otherUserId), attemptId, Ids.McQuestionId, optionId: Ids.McCorrectOptionId);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F4-07
    public async Task IT_F4_07_Save_answer_anonymous_is_401()
    {
        RequireDocker();
        var (_, attemptId) = await StartFreshAttempt();

        var res = await Save(App.Anonymous(), attemptId, Ids.McQuestionId, optionId: Ids.McCorrectOptionId);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F4-08
    public async Task IT_F4_08_Complete_returns_fast_without_ai_solution()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt();
        await Save(client, attemptId, Ids.FillBlankQuestionId, text: "999"); // sai

        var res = await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });
        await res.ShouldBeOk();

        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain("FullSolution", "AI feedback điền sau qua /result");
    }

    [SkippableFact] // IT-F4-09
    public async Task IT_F4_09_Grades_every_question_type()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt();
        await Save(client, attemptId, Ids.McQuestionId, optionId: Ids.McCorrectOptionId);
        await Save(client, attemptId, Ids.TfQuestionId, optionId: Ids.TfTrueOptionId);
        await Save(client, attemptId, Ids.FillBlankQuestionId, text: "0.5");
        await Save(client, attemptId, Ids.EssayQuestionId, text: "0 chia hết cho 2 nên là số chẵn.");

        var data = await (await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId }))
            .DataAsync();

        data.GetProperty("TotalScore").GetDouble().Should().Be(3);
        data.GetProperty("CorrectAnswers").GetInt32().Should().Be(3);
        data.GetProperty("WrongAnswers").GetInt32().Should().Be(0);
        data.GetProperty("HasPendingManualGrading").GetBoolean().Should().BeTrue();
    }

    [SkippableFact] // IT-F4-10
    public async Task IT_F4_10_Max_attempts_is_enforced()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt(maxAttempts: 1);
        await (await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId })).ShouldBeOk();

        var exId = await App.Db(db => db.ExerciseAttempts.Where(a => a.AttemptId == attemptId).Select(a => a.ExerciseId).FirstAsync());
        var again = await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId });

        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await again.RootAsync()).GetProperty("Message").GetString().Should().Contain("hết số lượt");
    }

    [SkippableFact] // IT-F4-11
    public async Task IT_F4_11_Completing_twice_in_parallel_grades_once()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt();
        await Save(client, attemptId, Ids.McQuestionId, optionId: Ids.McCorrectOptionId);

        var t1 = client.PostAsync("/api/exercise-attempts/complete", JsonContent.Create(new { AttemptId = attemptId }));
        var t2 = client.PostAsync("/api/exercise-attempts/complete", JsonContent.Create(new { AttemptId = attemptId }));
        var results = await Task.WhenAll(t1, t2);

        results.Count(r => r.IsSuccessStatusCode).Should().Be(1);
        results.Should().OnlyContain(r => (int)r.StatusCode < 500);
        await App.Db(async db =>
            (await db.ExerciseAttempts.SingleAsync(a => a.AttemptId == attemptId)).Status
                .Should().Be(AttemptStatus.Submitted));
    }

    [SkippableFact] // IT-F4-12
    public async Task IT_F4_12_Result_access_matrix()
    {
        RequireDocker();
        var studentAClient = App.AsRole(TestRole.StudentA);
        var exId = await Flow.PublishExerciseAsync();
        var start = await studentAClient.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId });
        var attemptId = await AttemptIdOf(start);
        await (await studentAClient.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId })).ShouldBeOk();

        (await studentAClient.GetAsync($"/api/exercise-attempts/{attemptId}/result")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await App.AsRole(TestRole.StudentB).GetAsync($"/api/exercise-attempts/{attemptId}/result")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await App.AsRole(TestRole.ParentLinked).GetAsync($"/api/exercise-attempts/{attemptId}/result")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await App.AsRole(TestRole.ParentStranger).GetAsync($"/api/exercise-attempts/{attemptId}/result")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F4-03
    public async Task IT_F4_03_Premium_student_can_start_a_premium_exercise()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        await Flow.GrantEntitlementAsync(studentId, EntitlementScope.AllContent, tier: PackageTier.Premium);
        var exId = await Flow.PublishExerciseAsync(tier: AccessTier.Premium);

        await (await App.As(userId).PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId })).ShouldBeOk();
    }

    [SkippableFact] // IT-F4-13
    public async Task IT_F4_13_History_access_matrix()
    {
        RequireDocker();
        var id = Ids.StudentAId;
        (await App.AsRole(TestRole.StudentA).GetAsync($"/api/exercise-attempts/student/{id}/history")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await App.AsRole(TestRole.StudentB).GetAsync($"/api/exercise-attempts/student/{id}/history")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await App.AsRole(TestRole.ParentLinked).GetAsync($"/api/exercise-attempts/student/{id}/history")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await App.AsRole(TestRole.ParentStranger).GetAsync($"/api/exercise-attempts/student/{id}/history")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F4-16
    public async Task IT_F4_16_Complete_after_planned_end_still_grades()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt();
        await Save(client, attemptId, Ids.McQuestionId, optionId: Ids.McCorrectOptionId);
        await App.Db(async db =>
        {
            var a = await db.ExerciseAttempts.SingleAsync(x => x.AttemptId == attemptId);
            a.PlannedEndTime = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        });

        var res = await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });
        await res.ShouldBeOk();
        await App.Db(async db =>
            (await db.ExerciseAttempts.SingleAsync(x => x.AttemptId == attemptId)).Status
                .Should().Be(AttemptStatus.Timeout));
    }

    [SkippableFact] // IT-F4-19
    public async Task IT_F4_19_Ai_hints_by_attempt_for_another_student_is_403()
    {
        RequireDocker();
        var (_, attemptId) = await StartFreshAttempt();
        var (otherUserId, _) = await Flow.NewStudentAsync();

        (await App.As(otherUserId).GetAsync($"/api/ai-hints/by-attempt/{attemptId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F4-17
    public async Task IT_F4_17_Removed_submit_route_is_404()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.StudentA).PostAsJsonAsync("/api/exercise-attempts/submit", new { AttemptId = 1 });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact] // IT-F4-04
    public async Task IT_F4_04_Start_random_saves_and_completes_without_a_phantom_timeout()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var client = App.As(userId);

        foreach (var duration in new int?[] { null, 15 })
        {
            var start = await client.PostAsJsonAsync("/api/exercise-attempts/start-random",
                new { BankId = Ids.BankId, NumberOfQuestions = 2, DurationMinutes = duration });
            await start.ShouldBeOk();
            var attemptId = await AttemptIdOf(start);

            await (await Save(client, attemptId, Ids.McQuestionId, optionId: Ids.McCorrectOptionId)).ShouldBeOk();

            var done = await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });
            await done.ShouldBeOk();

            await App.Db(async db =>
                (await db.ExerciseAttempts.SingleAsync(a => a.AttemptId == attemptId)).Status
                    .Should().Be(AttemptStatus.Submitted, $"duration={duration} — không bị đánh dấu Timeout ảo"));
        }
    }

    [SkippableFact] // IT-F4-14
    public async Task IT_F4_14_Feedback_status_reports_wrong_count_then_result_gets_full_solution()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt();
        await Save(client, attemptId, Ids.FillBlankQuestionId, text: "999"); // sai
        await (await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId })).ShouldBeOk();

        var status = await (await client.GetAsync($"/api/exercise-attempts/{attemptId}/feedback-status")).DataAsync();
        status.GetProperty("TotalWrong").GetInt32().Should().BeGreaterThan(0);

        var filled = false;
        for (var i = 0; i < 80 && !filled; i++)
        {
            await Task.Delay(250);
            var body = await (await client.GetAsync($"/api/exercise-attempts/{attemptId}/result")).Content.ReadAsStringAsync();
            filled = body.Contains("FullSolution");
        }
        filled.Should().BeTrue("job feedback nền (FakeAiService trả ngay) phải điền FullSolution vào /result");
    }

    [SkippableFact] // IT-F4-15
    public async Task IT_F4_15_Tab_switch_reports_are_debounced()
    {
        RequireDocker();
        var (client, attemptId) = await StartFreshAttempt();

        for (var i = 0; i < 6; i++)
            (await client.PostAsync($"/api/exercise-attempts/{attemptId}/report-tab-switch", null))
                .IsSuccessStatusCode.Should().BeTrue();

        await App.Db(async db =>
            (await db.TabSwitchLogs.CountAsync(l => l.AttemptId == attemptId))
                .Should().Be(1, "debounce 15s gộp cả loạt báo cáo dồn dập"));
    }

    [SkippableFact] // IT-F4-18
    public async Task IT_F4_18_Concurrent_starts_do_not_5xx()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var exId = await Flow.PublishExerciseAsync();
        var client = App.As(userId);

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.PostAsync("/api/exercise-attempts/start", JsonContent.Create(new { ExerciseId = exId })));
        var results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => (int)r.StatusCode < 500);
    }
}
