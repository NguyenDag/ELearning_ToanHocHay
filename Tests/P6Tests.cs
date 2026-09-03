using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// P6 — AI hint quota, parent linking + revoke, notification rule engine + prefs,
/// chatbot persistence, AI health.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class P6Tests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public P6Tests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    // ==================================================================
    // AI hint quota
    // ==================================================================
    [SkippableFact]
    public async Task Free_student_runs_out_of_AI_hints_after_the_daily_limit()
    {
        RequireDocker();
        await ResetHintUsageAsync(Id.StudentAId);
        await SetSubscriptionAsync(SubscriptionStatus.Pending); // student A = Free

        var client = _f.ClientFor(Id.StudentAUserId);
        var body = new { AttemptId = Id.AttemptAId, QuestionId = Id.McQuestionId, HintLevel = 1 };

        // First 3 consume the quota (the AI call itself fails in tests — no Flask — that's fine).
        for (var i = 0; i < 3; i++)
        {
            var r = await client.PostAsJsonAsync("/api/aihint", body);
            r.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var quota = await Root(await client.GetAsync("/api/aihint/quota"));
        quota.GetProperty("Used").GetInt32().Should().Be(3);
        quota.GetProperty("Remaining").GetInt32().Should().Be(0);

        (await client.PostAsJsonAsync("/api/aihint", body))
            .StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [SkippableFact]
    public async Task Unlimited_package_is_never_hint_rate_limited()
    {
        RequireDocker();
        await ResetHintUsageAsync(Id.StudentAId);
        await SetSubscriptionAsync(SubscriptionStatus.Active); // seed package has UnlimitedAiHint = true

        var client = _f.ClientFor(Id.StudentAUserId);
        var body = new { AttemptId = Id.AttemptAId, QuestionId = Id.McQuestionId, HintLevel = 1 };

        for (var i = 0; i < 6; i++)
            (await client.PostAsJsonAsync("/api/aihint", body))
                .StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

        (await Root(await client.GetAsync("/api/aihint/quota")))
            .GetProperty("Unlimited").GetBoolean().Should().BeTrue();

        await SetSubscriptionAsync(SubscriptionStatus.Pending);
    }

    // ==================================================================
    // Parent linking
    // ==================================================================
    [SkippableFact]
    public async Task Parent_links_a_child_by_code_and_revoke_drops_dashboard_access()
    {
        RequireDocker();

        var parentId = await _f.QueryDbAsync(db => db.Parents.AsNoTracking()
            .Where(p => p.UserId == Id.ParentUnlinkedUserId).Select(p => p.ParentId).FirstAsync());

        // parent mints an invite
        var inviteRes = await _f.ClientFor(Id.ParentUnlinkedUserId)
            .PostAsJsonAsync($"/api/parent/{parentId}/invites", new { Relationship = (int)ParentRelationship.Mother });
        inviteRes.StatusCode.Should().Be(HttpStatusCode.OK, await inviteRes.Content.ReadAsStringAsync());
        var token = (await Root(inviteRes)).GetProperty("Data").GetProperty("Token").GetString()!;

        // student B accepts
        var linkRes = await _f.ClientFor(Id.StudentBUserId)
            .PostAsJsonAsync("/api/parent/link", new { Code = token, Relationship = (int)ParentRelationship.Mother });
        linkRes.StatusCode.Should().Be(HttpStatusCode.OK, await linkRes.Content.ReadAsStringAsync());

        // parent can now see the child's dashboard + overview
        (await _f.ClientFor(Id.ParentUnlinkedUserId).GetAsync($"/api/student/{Id.StudentBId}/dashboard/overview"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await Root(await _f.ClientFor(Id.ParentUnlinkedUserId).GetAsync($"/api/parent/{parentId}/children/overview")))
            .GetProperty("Data").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        // revoke -> immediate loss of access
        (await _f.ClientFor(Id.ParentUnlinkedUserId)
            .DeleteAsync($"/api/parent/{parentId}/children/{Id.StudentBId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await _f.ClientFor(Id.ParentUnlinkedUserId).GetAsync($"/api/student/{Id.StudentBId}/dashboard/overview"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==================================================================
    // Notification rule engine
    // ==================================================================
    [SkippableFact]
    public async Task Low_score_notifies_the_student_and_the_linked_parent()
    {
        RequireDocker();
        var exerciseId = await BuildExerciseForStudentAAsync("lowscore");

        await SubmitZeroScoreAsync(exerciseId);

        var notes = await _f.QueryDbAsync(db => db.Notifications.AsNoTracking()
            .Where(n => n.Title == "Điểm bài làm thấp" && n.StudentId == Id.StudentAId)
            .Select(n => n.UserId).ToListAsync());

        var studentUserId = Id.StudentAUserId;
        var parentUserId = Id.ParentLinkedUserId; // seed: active link to student A

        notes.Should().Contain(studentUserId);
        notes.Should().Contain(parentUserId);
    }

    [SkippableFact]
    public async Task Opting_out_stops_that_rule_for_that_user_only()
    {
        RequireDocker();

        (await _f.ClientFor(Id.StudentAUserId).PutAsJsonAsync("/api/notifications/preferences",
            new { RuleKey = "low-score", Enabled = false })).StatusCode.Should().Be(HttpStatusCode.OK);

        var beforeId = await _f.QueryDbAsync(db => db.Notifications
            .Where(n => n.Title == "Điểm bài làm thấp")
            .Select(n => (int?)n.NotificationId).MaxAsync()) ?? 0;

        var exerciseId = await BuildExerciseForStudentAAsync("optout");
        await SubmitZeroScoreAsync(exerciseId);

        var newForStudent = await _f.QueryDbAsync(db => db.Notifications.CountAsync(n =>
            n.Title == "Điểm bài làm thấp" && n.UserId == Id.StudentAUserId && n.NotificationId > beforeId));
        newForStudent.Should().Be(0); // opted out

        var newForParent = await _f.QueryDbAsync(db => db.Notifications.CountAsync(n =>
            n.Title == "Điểm bài làm thấp" && n.UserId == Id.ParentLinkedUserId && n.NotificationId > beforeId));
        newForParent.Should().Be(1); // parent still notified

        // re-enable so other tests are unaffected
        await _f.ClientFor(Id.StudentAUserId).PutAsJsonAsync("/api/notifications/preferences",
            new { RuleKey = "low-score", Enabled = true });
    }

    [SkippableFact]
    public async Task Notification_endpoints_list_count_and_mark_read()
    {
        RequireDocker();
        var exerciseId = await BuildExerciseForStudentAAsync("notifapi");
        await SubmitZeroScoreAsync(exerciseId);

        var client = _f.ClientFor(Id.StudentAUserId);

        var list = await Root(await client.GetAsync("/api/notifications?unreadOnly=true"));
        var items = list.GetProperty("Data").GetProperty("Items");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        var firstId = items[0].GetProperty("NotificationId").GetInt32();

        (await client.PostAsync($"/api/notifications/{firstId}/read", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var stillUnread = await Root(await client.GetAsync("/api/notifications?unreadOnly=true"));
        stillUnread.GetProperty("Data").GetProperty("Items")
            .EnumerateArray().Select(x => x.GetProperty("NotificationId").GetInt32())
            .Should().NotContain(firstId);
    }

    // ==================================================================
    // Chatbot persistence + health
    // ==================================================================
    [SkippableFact]
    public async Task Chatbot_persists_the_turn_even_when_the_AI_is_down()
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentAUserId);

        var res = await client.PostAsJsonAsync("/api/chatbot/message", new { Text = "Xin chào" });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        var data = (await Root(res)).GetProperty("Data");
        data.GetProperty("AiAvailable").GetBoolean().Should().BeFalse(); // no Flask in tests
        var conversationId = data.GetProperty("ConversationId").GetInt32();

        var convos = await Root(await client.GetAsync("/api/chatbot/conversations"));
        convos.GetProperty("Data").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var msgs = await Root(await client.GetAsync($"/api/chatbot/conversations/{conversationId}/messages"));
        msgs.GetProperty("Data").GetArrayLength().Should().BeGreaterThanOrEqualTo(2); // user + system
    }

    [SkippableFact]
    public async Task Chatbot_health_is_503_when_the_AI_is_unreachable()
    {
        RequireDocker();
        (await _f.CreateClient().GetAsync("/api/chatbot/health"))
            .StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    // ==================================================================
    // helpers
    // ==================================================================
    private Task ResetHintUsageAsync(int studentId) => _f.QueryDbAsync(async db =>
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var row = await db.AiUsageDailies.FirstOrDefaultAsync(u => u.StudentId == studentId && u.Date == today);
        if (row != null) { row.HintCount = 0; await db.SaveChangesAsync(); }
        return true;
    });

    /// <summary>Puts student A on exactly one subscription in <paramref name="status"/> (others expired).</summary>
    private Task SetSubscriptionAsync(SubscriptionStatus status) => _f.QueryDbAsync(async db =>
    {
        var subs = await db.Subscriptions.Where(s => s.StudentId == Id.StudentAId).ToListAsync();
        foreach (var s in subs)
            s.Status = s.SubscriptionId == Id.SubscriptionAId ? status : SubscriptionStatus.Expired;

        var target = subs.Single(s => s.SubscriptionId == Id.SubscriptionAId);
        target.EndDate = DateTime.UtcNow.AddDays(30);
        await db.SaveChangesAsync();
        return true;
    });

    private async Task<int> BuildExerciseForStudentAAsync(string tag)
    {
        return await _f.QueryDbAsync(async db =>
        {
            var subjectId = await db.Subjects.Where(s => s.Code == "MATH").Select(s => s.SubjectId).FirstAsync();
            var gradeId = await db.GradeLevels.Where(g => g.Code == "G6").Select(g => g.GradeLevelId).FirstAsync();

            var fw = new CurriculumFramework { Code = $"P6-{tag}", Name = $"P6 {tag}" };
            db.CurriculumFrameworks.Add(fw);
            await db.SaveChangesAsync();

            var course = new Course
            {
                SubjectId = subjectId, GradeLevelId = gradeId, FrameworkId = fw.FrameworkId,
                Title = $"P6 {tag}", Slug = $"p6-{tag}", Status = CourseStatus.Published, CreatedBy = Id.ContentEditorUserId
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var version = new CourseVersion
            {
                CourseId = course.CourseId, VersionNumber = 1, State = VersionState.Published, PublishedAt = DateTime.UtcNow
            };
            db.CourseVersions.Add(version);
            await db.SaveChangesAsync();

            var exercise = new Exercise
            {
                ExerciseName = $"P6 ex {tag}", ExerciseType = ExerciseType.Quiz,
                Status = ExerciseStatus.Published, IsActive = true, IsFree = true,
                TotalQuestions = 1, TotalScores = 1, PassingScore = 1, CreatedBy = Id.ContentEditorUserId
            };
            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            db.ExerciseQuestions.Add(new ExerciseQuestion
            {
                ExerciseId = exercise.ExerciseId, QuestionId = Id.McQuestionId, Score = 1, OrderIndex = 1
            });
            db.StudentCourses.Add(new StudentCourse
            {
                StudentId = Id.StudentAId, CourseId = course.CourseId, CourseVersionId = version.CourseVersionId,
                Source = EnrollSource.Self, Status = StudentCourseStatus.Active, EnrolledAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return exercise.ExerciseId;
        });
    }

    private async Task SubmitZeroScoreAsync(int exerciseId)
    {
        var client = _f.ClientFor(Id.StudentAUserId);
        var start = await client.PostAsJsonAsync("/api/exerciseattempts/start", new { ExerciseId = exerciseId });
        start.StatusCode.Should().Be(HttpStatusCode.OK, await start.Content.ReadAsStringAsync());
        var attemptId = (await Root(start)).GetProperty("Data").GetProperty("AttemptId").GetInt32();

        var complete = await client.PostAsJsonAsync("/api/exerciseattempts/complete", new { AttemptId = attemptId });
        complete.StatusCode.Should().Be(HttpStatusCode.OK, await complete.Content.ReadAsStringAsync());
    }
}
