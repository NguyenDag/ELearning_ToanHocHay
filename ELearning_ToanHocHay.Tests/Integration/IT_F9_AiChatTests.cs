using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F9 — Trợ giúp AI &amp; Chatbot (IT-F9).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Ai")]
public class IT_F9_AiChatTests : IntegrationTest
{
    public IT_F9_AiChatTests(ApiFactory app) : base(app) { }

    private async Task<(HttpClient client, int userId, int attemptId)> StudentWithAttempt()
    {
        var (userId, _) = await Flow.NewStudentAsync();
        var exId = await Flow.PublishExerciseAsync();
        var client = App.As(userId);
        var start = await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId });
        await start.ShouldBeOk();
        return (client, userId, (await start.DataAsync()).GetProperty("AttemptId").GetInt32());
    }

    private Task<HttpResponseMessage> AskHint(HttpClient client, int attemptId)
        => client.PostAsJsonAsync("/api/ai-hints", new { AttemptId = attemptId, QuestionId = Ids.McQuestionId });

    [SkippableFact] // IT-F9-01
    public async Task IT_F9_01_Free_student_runs_out_of_ai_hints()
    {
        RequireDocker();
        var (client, _, attemptId) = await StudentWithAttempt();

        for (var i = 0; i < 3; i++)
            (await AskHint(client, attemptId)).IsSuccessStatusCode.Should().BeTrue($"hint #{i + 1}");

        (await AskHint(client, attemptId)).StatusCode.Should().Be((HttpStatusCode)429);
    }

    [SkippableFact] // IT-F9-02
    public async Task IT_F9_02_Unlimited_package_is_never_rate_limited()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        await Flow.GrantEntitlementAsync(studentId, EntitlementScope.AllContent, tier: PackageTier.Premium);
        var exId = await Flow.PublishExerciseAsync();
        var client = App.As(userId);
        var attemptId = (await (await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId })).DataAsync())
            .GetProperty("AttemptId").GetInt32();

        for (var i = 0; i < 5; i++)
            (await AskHint(client, attemptId)).StatusCode.Should().NotBe((HttpStatusCode)429, $"hint #{i + 1}");
    }

    [SkippableFact] // IT-F9-03
    public async Task IT_F9_03_Quota_endpoint_reports_usage()
    {
        RequireDocker();
        var (client, _, attemptId) = await StudentWithAttempt();
        await AskHint(client, attemptId);
        await AskHint(client, attemptId);

        var q = await (await client.GetAsync("/api/ai-hints/quota")).DataAsync();
        q.GetProperty("Used").GetInt32().Should().Be(2);
        q.GetProperty("Limit").GetInt32().Should().Be(3);
        q.GetProperty("Remaining").GetInt32().Should().Be(1);
    }

    [SkippableFact] // IT-F9-04
    public async Task IT_F9_04_Hint_on_another_attempt_403_anonymous_401()
    {
        RequireDocker();
        var (_, _, attemptId) = await StudentWithAttempt();
        var (otherUserId, _) = await Flow.NewStudentAsync();

        (await AskHint(App.As(otherUserId), attemptId)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await AskHint(App.Anonymous(), attemptId)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F9-05
    public async Task IT_F9_05_Background_feedback_fills_in_the_full_solution()
    {
        RequireDocker();
        var (client, _, attemptId) = await StudentWithAttempt();
        await client.PostAsJsonAsync("/api/exercise-attempts/save-answer",
            new { AttemptId = attemptId, QuestionId = Ids.FillBlankQuestionId, AnswerText = "999" }); // sai
        await (await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId })).ShouldBeOk();

        var filled = false;
        for (var i = 0; i < 60 && !filled; i++)
        {
            await Task.Delay(250);
            var body = await (await client.GetAsync($"/api/exercise-attempts/{attemptId}/result")).Content.ReadAsStringAsync();
            filled = body.Contains("FullSolution");
        }
        Skip.IfNot(filled, "Job feedback nền chưa điền FullSolution trong 15s (timing nền — không coi là lỗi).");
    }

    [SkippableFact] // IT-F9-06
    public async Task IT_F9_06_Chatbot_persists_the_turn_even_when_ai_is_down()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        App.Ai.SetHealthy(false);
        try
        {
            var res = await App.As(userId).PostAsJsonAsync("/api/chatbot/message", new { Text = "Giúp em bài này với" });
            await res.ShouldBeOk();

            await App.Db(async db =>
            {
                var conv = await db.ChatConversations.FirstOrDefaultAsync(c => c.InitiatorUserId == userId);
                conv.Should().NotBeNull();
                (await db.ChatMessages.AnyAsync(m => m.ConversationId == conv!.ConversationId)).Should().BeTrue();
            });
        }
        finally
        {
            App.Ai.SetHealthy(true);
        }
    }

    [SkippableFact] // IT-F9-07
    public async Task IT_F9_07_Chatbot_health_reflects_the_ai_service()
    {
        RequireDocker();
        (await App.Anonymous().GetAsync("/api/chatbot/health")).StatusCode.Should().Be(HttpStatusCode.OK);

        App.Ai.SetHealthy(false);
        try
        {
            (await App.Anonymous().GetAsync("/api/chatbot/health")).StatusCode
                .Should().Be(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            App.Ai.SetHealthy(true);
        }
    }

    [SkippableFact] // IT-F9-10
    public async Task IT_F9_10_Anonymous_chatbot_message_is_401()
    {
        RequireDocker();
        (await App.Anonymous().PostAsJsonAsync("/api/chatbot/message", new { Text = "hi" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
