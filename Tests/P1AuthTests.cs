using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// P1 — account &amp; auth lifecycle: refresh-token rotation, login throttling,
/// forgot/reset password, admin lock/unlock + role change with audit log.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class P1AuthTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public P1AuthTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private const string Pwd = "Password123!";

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>Creates a confirmed Student user with a known password; returns its email.</summary>
    private async Task<string> NewLoginUserAsync(string tag)
    {
        var email = $"p1-{tag}-{Guid.NewGuid():N}@test.local";
        await _f.QueryDbAsync(async db =>
        {
            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Pwd),
                FullName = $"P1 {tag}",
                UserType = UserType.Student,
                IsEmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            db.Students.Add(new Student { UserId = user.UserId });
            await db.SaveChangesAsync();
            return true;
        });
        return email;
    }

    private async Task<(string access, string refresh)> LoginAsync(string email)
    {
        var res = await _f.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = Pwd });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        var data = (await Root(res)).GetProperty("Data");
        return (data.GetProperty("Token").GetString()!, data.GetProperty("RefreshToken").GetString()!);
    }

    [SkippableFact]
    public async Task Login_issues_a_refresh_token_and_rotation_revokes_the_old_one()
    {
        RequireDocker();
        var email = await NewLoginUserAsync("rotate");
        var (_, refresh1) = await LoginAsync(email);

        var client = _f.CreateClient();

        var r1 = await client.PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = refresh1 });
        r1.StatusCode.Should().Be(HttpStatusCode.OK, await r1.Content.ReadAsStringAsync());
        var refresh2 = (await Root(r1)).GetProperty("Data").GetProperty("RefreshToken").GetString()!;
        refresh2.Should().NotBe(refresh1);

        // the new one works
        var r2 = await client.PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = refresh2 });
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        var refresh3 = (await Root(r2)).GetProperty("Data").GetProperty("RefreshToken").GetString()!;

        // replaying a rotated-away token is treated as reuse: rejected AND every session is cut
        (await client.PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = refresh1 }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = refresh3 }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Change_password_revokes_every_refresh_token()
    {
        RequireDocker();
        var email = await NewLoginUserAsync("chpwd");
        var (access, refresh) = await LoginAsync(email);

        var client = _f.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);

        var ch = await client.PostAsJsonAsync("/api/auth/change-password",
            new { CurrentPassword = Pwd, NewPassword = "NewPassword456!" });
        ch.StatusCode.Should().Be(HttpStatusCode.OK, await ch.Content.ReadAsStringAsync());

        (await _f.CreateClient().PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = refresh }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Five_failed_logins_lock_the_account_temporarily()
    {
        RequireDocker();
        var email = await NewLoginUserAsync("lockout");
        var client = _f.CreateClient();

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "wrong" });

        // even the correct password is refused while locked out
        var res = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = Pwd });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Root(res)).GetProperty("Message").GetString().Should().Contain("khoá");

        var locked = await _f.QueryDbAsync(db => db.Users.AsNoTracking()
            .Where(u => u.Email == email).Select(u => u.LockoutEndsAt).FirstAsync());
        locked.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Forgot_password_is_always_ok_and_reset_changes_the_password()
    {
        RequireDocker();
        var email = await NewLoginUserAsync("reset");

        var client = _f.CreateClient();

        // unknown email still returns 200 (no account enumeration)
        (await client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "nobody@test.local" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await _f.QueryDbAsync(db => db.PasswordResetTokens.AsNoTracking()
            .Where(t => t.User!.Email == email && !t.IsUsed)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.Token).FirstAsync());

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { Token = token, NewPassword = "BrandNew789!" });
        reset.StatusCode.Should().Be(HttpStatusCode.OK, await reset.Content.ReadAsStringAsync());

        (await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = Pwd }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "BrandNew789!" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Admin_lock_blocks_login_and_unlock_restores_it()
    {
        RequireDocker();
        var email = await NewLoginUserAsync("adminlock");
        var targetId = await _f.QueryDbAsync(db => db.Users.AsNoTracking()
            .Where(u => u.Email == email).Select(u => u.UserId).FirstAsync());

        var admin = _f.ClientFor(Id.AdminUserId);

        (await admin.PostAsJsonAsync($"/api/admin/users/{targetId}/lock", new { Reason = "test" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await _f.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = Pwd }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await admin.PostAsync($"/api/admin/users/{targetId}/unlock", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await _f.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = Pwd }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Role_change_is_audited_and_forbidden_for_non_admins()
    {
        RequireDocker();

        (await _f.ClientFor(Id.ContentEditorUserId)
            .PostAsJsonAsync($"/api/admin/users/{Id.FinanceUserId}/role", new { NewRole = (int)UserType.SystemAdmin }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var res = await _f.ClientFor(Id.AdminUserId)
            .PostAsJsonAsync($"/api/admin/users/{Id.FinanceUserId}/role", new { NewRole = (int)UserType.ContentEditor });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());

        var audited = await _f.QueryDbAsync(db => db.AuditLogs.AsNoTracking()
            .AnyAsync(l => l.Action == "ChangeRole" && l.EntityId == Id.FinanceUserId));
        audited.Should().BeTrue();

        // put it back so other tests see Finance as Finance
        await _f.ClientFor(Id.AdminUserId)
            .PostAsJsonAsync($"/api/admin/users/{Id.FinanceUserId}/role", new { NewRole = (int)UserType.FinanceManager });
    }
}
