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
        for (var i = 0; i < 80 && !filled; i++)
        {
            await Task.Delay(250);
            var body = await (await client.GetAsync($"/api/exercise-attempts/{attemptId}/result")).Content.ReadAsStringAsync();
            filled = body.Contains("FullSolution");
        }
        filled.Should().BeTrue("job feedback nền (FakeAiService trả ngay) phải điền FullSolution vào /result");
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

    /// <summary>Học sinh mới có 1 hội thoại đang mở — trả <c>(client, conversationId)</c>.</summary>
    private async Task<(HttpClient student, int conversationId)> StudentWithOpenConversation()
    {
        var (userId, _) = await Flow.NewStudentAsync();
        var client = App.As(userId);
        await (await client.PostAsJsonAsync("/api/chatbot/message", new { Text = "Em cần hỏi bài" })).ShouldBeOk();
        var convId = await App.Db(db => db.ChatConversations
            .Where(c => c.InitiatorUserId == userId)
            .Select(c => c.ConversationId).FirstAsync());
        return (client, convId);
    }

    [SkippableFact] // IT-F9-08
    public async Task IT_F9_08_Request_human_moves_the_conversation_into_the_staff_queue()
    {
        RequireDocker();
        var (student, convId) = await StudentWithOpenConversation();

        await (await student.PostAsync("/api/chatbot/request-human", null)).ShouldBeOk();

        await App.Db(async db =>
            (await db.ChatConversations.SingleAsync(c => c.ConversationId == convId)).Status
                .Should().Be(ChatStatus.WaitingAgent));

        var queue = await (await App.AsRole(TestRole.Support).GetAsync("/api/chatbot/staff/queue")).DataAsync();
        queue.EnumerateArray().Select(c => c.GetProperty("ConversationId").GetInt32()).Should().Contain(convId);
    }

    [SkippableFact] // IT-F9-09
    public async Task IT_F9_09_Staff_can_assign_reply_close_but_non_staff_cannot()
    {
        RequireDocker();
        var (student, convId) = await StudentWithOpenConversation();
        await (await student.PostAsync("/api/chatbot/request-human", null)).ShouldBeOk();
        var staff = App.AsRole(TestRole.Support);

        // non-staff → 403
        (await student.PostAsync($"/api/chatbot/staff/conversations/{convId}/assign", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await (await staff.PostAsync($"/api/chatbot/staff/conversations/{convId}/assign", null)).ShouldBeOk();
        await (await staff.PostAsJsonAsync($"/api/chatbot/staff/conversations/{convId}/reply", new { Text = "Chào em, thầy hỗ trợ nhé" })).ShouldBeOk();
        await (await staff.PostAsync($"/api/chatbot/conversations/{convId}/close", null)).ShouldBeOk();

        await App.Db(async db =>
            (await db.ChatConversations.SingleAsync(c => c.ConversationId == convId)).Status
                .Should().Be(ChatStatus.Closed));
    }
}
