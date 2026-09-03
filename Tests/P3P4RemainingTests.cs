using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>Remaining P3 (concurrent submit) + P4 (weekly comparison) regression tests.</summary>
[Collection(IntegrationCollection.Name)]
public class P3P4RemainingTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public P3P4RemainingTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    [SkippableFact]
    public async Task Completing_an_attempt_twice_in_parallel_grades_it_once()
    {
        RequireDocker();

        // a fresh in-progress attempt for student A
        var exerciseId = await _f.QueryDbAsync(db => db.Exercises.AsNoTracking()
            .Where(e => e.ExerciseName == "Graded quiz").Select(e => e.ExerciseId).FirstAsync());

        var client = _f.ClientFor(Id.StudentAUserId);
        var start = await client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exerciseId });
        // start resumes the seeded in-progress attempt or makes a new one — either way it is InProgress
        var attemptId = (await Root(start)).GetProperty("Data").GetProperty("AttemptId").GetInt32();

        var body = new { AttemptId = attemptId };
        var t1 = client.PostAsJsonAsync("/api/exercise-attempts/complete", body);
        var t2 = _f.ClientFor(Id.StudentAUserId).PostAsJsonAsync("/api/exercise-attempts/complete", body);
        var results = await Task.WhenAll(t1, t2);

        results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        results.Count(r => r.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);

        var attempts = await _f.QueryDbAsync(db => db.ExerciseAttempts.AsNoTracking()
            .CountAsync(a => a.AttemptId == attemptId && a.Status == AttemptStatus.Submitted));
        attempts.Should().Be(1);
    }

    [SkippableFact]
    public async Task Weekly_stats_compare_this_week_against_last_week()
    {
        RequireDocker();

        var exerciseId = await _f.QueryDbAsync(db => db.Exercises.AsNoTracking()
            .Where(e => e.ExerciseName == "Graded quiz").Select(e => e.ExerciseId).FirstAsync());

        // a dedicated student so other tests' attempts don't skew the averages
        var (userId, studentId) = await _f.QueryDbAsync(async db =>
        {
            var u = new User
            {
                Email = $"p4-week-{Guid.NewGuid():N}@test.local", PasswordHash = "x", FullName = "P4 week",
                UserType = UserType.Student, IsEmailConfirmed = true, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            var s = new Student { UserId = u.UserId };
            db.Students.Add(s);
            await db.SaveChangesAsync();
            return (u.UserId, s.StudentId);
        });

        // fixed data: 2 attempts last week (avg 4/10), 3 this week (avg 8/10)
        await _f.QueryDbAsync(async db =>
        {
            var now = DateTime.UtcNow;
            DateTime WeekStart(DateTime d)
            {
                var diff = (7 + (d.DayOfWeek - DayOfWeek.Monday)) % 7;
                return d.AddDays(-diff).Date;
            }
            var thisWeek = WeekStart(now).AddDays(1);
            var lastWeek = WeekStart(now).AddDays(-6);

            void Add(DateTime at, double score) => db.ExerciseAttempts.Add(new ExerciseAttempt
            {
                StudentId = studentId, ExerciseId = exerciseId,
                StartTime = at, SubmittedAt = at.AddMinutes(10),
                Status = AttemptStatus.Submitted, TotalScore = score, MaxScore = 10
            });
            Add(lastWeek, 4); Add(lastWeek.AddHours(2), 4);
            Add(thisWeek, 8); Add(thisWeek.AddHours(1), 8); Add(thisWeek.AddHours(2), 8);

            db.DailyActivitySnapshots.Add(new DailyActivitySnapshot
            { StudentId = studentId, Date = DateOnly.FromDateTime(thisWeek), ExercisesDone = 3, MinutesStudied = 30 });
            await db.SaveChangesAsync();
            return true;
        });

        var res = await _f.ClientFor(userId).GetAsync($"/api/students/{studentId}/dashboard/overview");
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());

        var stats = (await Root(res)).GetProperty("Stats");
        stats.GetProperty("WeeklyExercisesCompleted").GetInt32().Should().Be(3);
        var cmp = stats.GetProperty("WeekComparison");
        cmp.GetProperty("ExerciseCountChange").GetInt32().Should().Be(1);  // 3 this week - 2 last week
        cmp.GetProperty("ScoreChange").GetInt32().Should().BeGreaterThan(0); // 8 vs 4
    }
}
