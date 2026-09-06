using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F1 — Xác thực &amp; tài khoản (IT-F1).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Auth")]
public class IT_F1_AuthTests : IntegrationTest
{
    public IT_F1_AuthTests(ApiFactory app) : base(app) { }

    private HttpClient Anon() => App.Anonymous();

    [SkippableFact] // IT-F1-01
    public async Task IT_F1_01_Register_student_creates_unconfirmed_user()
    {
        RequireDocker();
        var email = $"new.{Guid.NewGuid():N}@flow.test";

        var res = await Anon().PostAsJsonAsync("/api/auth/register", new
        {
            Email = email, Password = "Test!234", ConfirmPassword = "Test!234",
            FullName = "Học Sinh Mới", UserType = "Student",
        });

        await res.ShouldBeOk();
        await App.Db(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Email == email);
            user.IsEmailConfirmed.Should().BeFalse();
            (await db.Students.AnyAsync(s => s.UserId == user.UserId)).Should().BeTrue();
            (await db.EmailVerificationTokens.AnyAsync(t => t.UserId == user.UserId)).Should().BeTrue();
        });
        (await res.DataAsync()).ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Object,
            "register không trả token đăng nhập");
    }

    [SkippableFact] // IT-F1-02
    public async Task IT_F1_02_Register_duplicate_confirmed_email_is_rejected()
    {
        RequireDocker();
        var res = await Anon().PostAsJsonAsync("/api/auth/register", new
        {
            Email = "student.a@it.test", Password = "Test!234", ConfirmPassword = "Test!234",
            FullName = "Trùng", UserType = "Student",
        });
        await res.ShouldBeError(HttpStatusCode.BadRequest, "Email đã được đăng ký");
    }

    [SkippableFact] // IT-F1-03
    public async Task IT_F1_03_Register_privileged_role_is_rejected_and_rolled_back()
    {
        RequireDocker();
        var email = $"admin.{Guid.NewGuid():N}@flow.test";
        var res = await Anon().PostAsJsonAsync("/api/auth/register", new
        {
            Email = email, Password = "Test!234", ConfirmPassword = "Test!234",
            FullName = "Kẻ Gian", UserType = "SystemAdmin",
        });

        await res.ShouldBeError(HttpStatusCode.BadRequest, "Không cho phép đăng ký role này");
        await App.Db(async db => (await db.Users.AnyAsync(u => u.Email == email)).Should().BeFalse());
    }

    [SkippableFact] // IT-F1-04
    public async Task IT_F1_04_Confirm_email_with_a_valid_token()
    {
        RequireDocker();
        var (userId, _, token) = await Flow.NewUnconfirmedUserAsync(UserType.Student);

        var res = await Anon().GetAsync($"/api/auth/confirm-email?token={token}");

        await res.ShouldBeOk();
        await App.Db(async db =>
            (await db.Users.SingleAsync(u => u.UserId == userId)).IsEmailConfirmed.Should().BeTrue());
    }

    [SkippableFact] // IT-F1-05
    public async Task IT_F1_05_Confirm_email_with_a_bad_token_is_rejected()
    {
        RequireDocker();
        var res = await Anon().GetAsync("/api/auth/confirm-email?token=not-a-real-token");
        await res.ShouldBeError(HttpStatusCode.BadRequest, "Liên kết không hợp lệ");
    }

    [SkippableFact] // IT-F1-08
    public async Task IT_F1_08_Login_returns_token_pair_and_tier()
    {
        RequireDocker();
        var res = await Anon().PostAsJsonAsync("/api/auth/login", new
        {
            Email = "student.a@it.test", Password = "Test!234",
        });

        await res.ShouldBeOk();
        var data = await res.DataAsync();
        data.GetProperty("Token").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("RefreshToken").GetString().Should().NotBeNullOrEmpty();
        data.TryGetProperty("PackageTier", out _).Should().BeTrue();
    }

    [SkippableFact] // IT-F1-09
    public async Task IT_F1_09_Wrong_password_increments_failure_count()
    {
        RequireDocker();
        var (userId, email, _) = await Flow.NewConfirmedUserAsync(UserType.Student);

        var res = await Anon().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "wrong-one" });

        res.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
        await App.Db(async db =>
            (await db.Users.SingleAsync(u => u.UserId == userId)).FailedLoginCount.Should().Be(1));
    }

    [SkippableFact] // IT-F1-10
    public async Task IT_F1_10_Unconfirmed_email_cannot_login()
    {
        RequireDocker();
        var (_, email, _) = await Flow.NewUnconfirmedUserAsync(UserType.Student);

        var res = await Anon().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test!234" });

        res.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
        (await res.RootAsync()).GetProperty("Message").GetString().Should().Contain("xác nhận email");
    }

    [SkippableFact] // IT-F1-11
    public async Task IT_F1_11_Five_failed_logins_lock_the_account()
    {
        RequireDocker();
        var (userId, email, _) = await Flow.NewConfirmedUserAsync(UserType.Student);

        for (var i = 0; i < 5; i++)
            await Anon().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "nope" });

        var locked = await Anon().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test!234" });
        (await locked.RootAsync()).GetProperty("Message").GetString().Should().Contain("tạm khoá");
        await App.Db(async db =>
            (await db.Users.SingleAsync(u => u.UserId == userId)).LockoutEndsAt.Should().NotBeNull());
    }

    [SkippableFact] // IT-F1-13 / IT-F1-14 — refresh rotation + reuse detection
    public async Task IT_F1_13_Refresh_rotates_and_reuse_is_detected()
    {
        RequireDocker();
        var (_, email, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        var login = await (await Anon().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test!234" }))
            .DataAsync();
        var refresh1 = login.GetProperty("RefreshToken").GetString()!;

        var rotated = await Anon().PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = refresh1 });
        await rotated.ShouldBeOk();

        // reuse the now-rotated token → rejected
        var reuse = await Anon().PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = refresh1 });
        reuse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F1-15
    public async Task IT_F1_15_Garbage_refresh_token_is_401_not_500()
    {
        RequireDocker();
        var res = await Anon().PostAsJsonAsync("/api/auth/refresh-token", new { RefreshToken = "not.a.token" });
        res.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F1-18
    public async Task IT_F1_18_Forgot_password_creates_a_reset_token()
    {
        RequireDocker();
        var (userId, email, _) = await Flow.NewConfirmedUserAsync(UserType.Student);

        var res = await Anon().PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });

        await res.ShouldBeOk();
        await App.Db(async db =>
            (await db.PasswordResetTokens.AnyAsync(t => t.UserId == userId && !t.IsUsed)).Should().BeTrue());
    }

    [SkippableFact] // IT-F1-19
    public async Task IT_F1_19_Forgot_password_for_unknown_email_does_not_leak()
    {
        RequireDocker();
        var res = await Anon().PostAsJsonAsync("/api/auth/forgot-password", new { Email = "ghost@flow.test" });
        await res.ShouldBeOk();
    }

    [SkippableFact] // IT-F1-20
    public async Task IT_F1_20_Reset_password_then_login_with_the_new_one()
    {
        RequireDocker();
        var (_, email, _) = await Flow.NewConfirmedUserAsync(UserType.Student);
        await Anon().PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });
        var token = App.Email.LastToken("reset");
        token.Should().NotBeNull();

        var reset = await Anon().PostAsJsonAsync("/api/auth/reset-password", new { Token = token, NewPassword = "Brand!New9" });
        await reset.ShouldBeOk();

        var login = await Anon().PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Brand!New9" });
        await login.ShouldBeOk();

        var second = await Anon().PostAsJsonAsync("/api/auth/reset-password", new { Token = token, NewPassword = "Another!1" });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact] // IT-F1-23
    public async Task IT_F1_23_Anonymous_me_is_401_not_302()
    {
        RequireDocker();
        var res = await Anon().GetAsync("/api/auth/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
