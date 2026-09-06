using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F10 — Thông báo (IT-F10).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Notification")]
public class IT_F10_NotificationTests : IntegrationTest
{
    public IT_F10_NotificationTests(ApiFactory app) : base(app) { }

    /// <summary>Học sinh + phụ huynh liên kết, học sinh nộp một bài điểm 0/10.</summary>
    private async Task<(int studentUserId, int parentUserId, int studentId)> LinkedPairThatFailedAnExercise()
    {
        var (studentUserId, studentId) = await Flow.NewStudentAsync();
        var (parentUserId, _, code) = await Flow.NewParentAsync();
        await App.As(studentUserId).PostAsJsonAsync("/api/parents/link", new { Code = code });

        var exId = await Flow.PublishExerciseAsync();
        var client = App.As(studentUserId);
        var attemptId = (await (await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId })).DataAsync())
            .GetProperty("AttemptId").GetInt32();
        // không trả lời gì -> điểm 0
        await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });
        return (studentUserId, parentUserId, studentId);
    }

    [SkippableFact] // IT-F10-01
    public async Task IT_F10_01_Low_score_notifies_student_and_linked_parent()
    {
        RequireDocker();
        var (studentUserId, parentUserId, _) = await LinkedPairThatFailedAnExercise();

        await App.Db(async db =>
        {
            (await db.Notifications.AnyAsync(n => n.UserId == studentUserId && n.Title.Contains("thấp"))).Should().BeTrue();
            (await db.Notifications.AnyAsync(n => n.UserId == parentUserId && n.Title.Contains("thấp"))).Should().BeTrue();
        });
    }

    [SkippableFact] // IT-F10-04
    public async Task IT_F10_04_Opting_out_stops_that_rule_for_that_user_only()
    {
        RequireDocker();
        var (studentUserId, studentId) = await Flow.NewStudentAsync();
        var (parentUserId, _, code) = await Flow.NewParentAsync();
        await App.As(studentUserId).PostAsJsonAsync("/api/parents/link", new { Code = code });

        await (await App.As(parentUserId).PutAsJsonAsync("/api/notifications/preferences",
            new { RuleKey = "low-score", Enabled = false })).ShouldBeOk();

        var exId = await Flow.PublishExerciseAsync();
        var client = App.As(studentUserId);
        var attemptId = (await (await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId })).DataAsync())
            .GetProperty("AttemptId").GetInt32();
        await client.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId });

        await App.Db(async db =>
        {
            (await db.Notifications.AnyAsync(n => n.UserId == studentUserId && n.Title.Contains("thấp"))).Should().BeTrue();
            (await db.Notifications.AnyAsync(n => n.UserId == parentUserId && n.Title.Contains("thấp"))).Should().BeFalse();
        });
    }

    [SkippableFact] // IT-F10-05
    public async Task IT_F10_05_List_count_mark_read()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var n1 = await Flow.SeedNotificationAsync(userId, "A");
        await Flow.SeedNotificationAsync(userId, "B");
        var client = App.As(userId);

        (await (await client.GetAsync("/api/notifications")).ShouldBePaged()).Total.Should().Be(2);
        (await (await client.GetAsync("/api/notifications/unread-count")).DataAsync()).GetInt32().Should().Be(2);

        await (await client.PostAsync($"/api/notifications/{n1}/read", null)).ShouldBeOk();
        (await (await client.GetAsync("/api/notifications/unread-count")).DataAsync()).GetInt32().Should().Be(1);

        await (await client.PostAsync("/api/notifications/read-all", null)).ShouldBeOk();
        (await (await client.GetAsync("/api/notifications/unread-count")).DataAsync()).GetInt32().Should().Be(0);
    }

    [SkippableFact] // IT-F10-06
    public async Task IT_F10_06_Preferences_endpoint_shape()
    {
        RequireDocker();
        var data = await (await App.AsRole(TestRole.StudentA).GetAsync("/api/notifications/preferences")).DataAsync();
        data.EnumerateArray().Select(x => x.GetProperty("RuleKey").GetString())
            .Should().Contain(new[] { "tab-switch", "low-score", "inactivity" });
    }

    [SkippableFact] // IT-F10-07
    public async Task IT_F10_07_Marking_another_users_notification_read_fails()
    {
        RequireDocker();
        var (ownerId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var nid = await Flow.SeedNotificationAsync(ownerId);
        var (strangerId, _, _) = await Flow.NewConfirmedUserAsync(UserType.Student);

        (await App.As(strangerId).PostAsync($"/api/notifications/{nid}/read", null))
            .IsSuccessStatusCode.Should().BeFalse();
    }
}
