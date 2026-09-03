using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>P7 — ops &amp; quality: health, correlation id, pagination, audit interceptor.</summary>
[Collection(IntegrationCollection.Name)]
public class P7Tests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public P7Tests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    [SkippableFact]
    public async Task Health_endpoints_report_live_and_ready()
    {
        RequireDocker();
        (await _f.CreateClient().GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _f.CreateClient().GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Every_response_carries_a_correlation_id_and_echoes_an_inbound_one()
    {
        RequireDocker();

        var res1 = await _f.CreateClient().GetAsync("/health");
        res1.Headers.TryGetValues("X-Correlation-ID", out var generated).Should().BeTrue();
        generated!.Single().Should().NotBeNullOrWhiteSpace();

        var client = _f.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/health");
        req.Headers.Add("X-Correlation-ID", "test-correlation-123");
        var res2 = await client.SendAsync(req);
        res2.Headers.GetValues("X-Correlation-ID").Single().Should().Be("test-correlation-123");
    }

    [SkippableFact]
    public async Task User_list_is_paged_and_searchable()
    {
        RequireDocker();
        var admin = _f.ClientFor(Id.AdminUserId);

        var paged = await Root(await admin.GetAsync("/api/users?page=1&pageSize=2"));
        var data = paged.GetProperty("Data");
        data.GetProperty("Items").GetArrayLength().Should().Be(2);
        data.GetProperty("PageSize").GetInt32().Should().Be(2);
        data.GetProperty("Total").GetInt32().Should().BeGreaterThan(2);

        var search = await Root(await admin.GetAsync("/api/users?search=admin@test.local"));
        var items = search.GetProperty("Data").GetProperty("Items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("Email").GetString().Should().Be("admin@test.local");
    }

    [SkippableFact]
    public async Task Subscription_list_is_paged_and_status_filtered()
    {
        RequireDocker();
        var finance = _f.ClientFor(Id.FinanceUserId);

        var res = await finance.GetAsync("/api/subscriptions?page=1&pageSize=5&status=Pending");
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        var items = (await Root(res)).GetProperty("Data").GetProperty("Items");
        foreach (var s in items.EnumerateArray())
            s.GetProperty("Status").GetString().Should().Be(nameof(SubscriptionStatus.Pending));
    }

    [SkippableFact]
    public async Task Sensitive_field_change_is_written_to_the_audit_log_by_the_interceptor()
    {
        RequireDocker();

        // fresh target so we don't collide with other tests
        var email = $"p7-audit-{Guid.NewGuid():N}@test.local";
        var targetId = await _f.QueryDbAsync(async db =>
        {
            var u = new User
            {
                Email = email, PasswordHash = "x", FullName = "P7 audit",
                UserType = UserType.Student, IsEmailConfirmed = true, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            db.Students.Add(new Student { UserId = u.UserId });
            await db.SaveChangesAsync();
            return u.UserId;
        });

        (await _f.ClientFor(Id.AdminUserId).PostAsJsonAsync($"/api/admin/users/{targetId}/lock", new { Reason = "p7" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var audited = await _f.QueryDbAsync(db => db.AuditLogs.AsNoTracking().AnyAsync(l =>
            l.EntityType == "User" && l.EntityId == targetId && l.Action == "Update"
            && l.NewValueJson != null && l.NewValueJson.Contains("LockedAt")));
        audited.Should().BeTrue();
    }

    [SkippableFact]
    public async Task Locking_a_user_invalidates_their_in_flight_access_token()
    {
        RequireDocker();

        var pwd = "Password123!";
        var email = $"p7-sstamp-{Guid.NewGuid():N}@test.local";
        var userId = await _f.QueryDbAsync(async db =>
        {
            var u = new User
            {
                Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd), FullName = "P7 sstamp",
                UserType = UserType.Student, IsEmailConfirmed = true, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            db.Students.Add(new Student { UserId = u.UserId });
            await db.SaveChangesAsync();
            return u.UserId;
        });

        var anon = _f.CreateClient();
        var login = await anon.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = pwd });
        var access = (await Root(login)).GetProperty("Data").GetProperty("Token").GetString()!;

        var client = _f.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _f.ClientFor(Id.AdminUserId).PostAsJsonAsync($"/api/admin/users/{userId}/lock", new { Reason = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
