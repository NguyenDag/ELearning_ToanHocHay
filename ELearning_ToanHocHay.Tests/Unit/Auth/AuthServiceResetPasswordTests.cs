using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.5 — UT-AUTH-RESET-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class AuthServiceResetPasswordTests : AuthU2Base
{
    private PasswordResetToken AddResetToken(int userId, Action<PasswordResetToken>? tweak = null)
    {
        var token = new PasswordResetToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString("N"),
            ExpiredAt = Now.AddHours(1),
            IsUsed = false,
        };
        tweak?.Invoke(token);
        Sql.Db.PasswordResetTokens.Add(token);
        Sql.Db.SaveChanges();
        return token;
    }

    // ---- ForgotPassword ----

    [Fact] // UT-AUTH-RESET-01
    public async Task Forgot_unknown_email_is_fuzzy_success_without_token()
    {
        var res = await Build().ForgotPasswordAsync("ghost@test.local");

        res.Success.Should().BeTrue();
        Sql.NewContext().PasswordResetTokens.Should().BeEmpty();
        BgEmail.DidNotReceive().QueuePasswordResetEmail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact] // UT-AUTH-RESET-02
    public async Task Forgot_unconfirmed_user_does_not_leak_or_send()
    {
        var u = AddUser(x => x.IsEmailConfirmed = false);
        await Build().ForgotPasswordAsync(u.Email);

        Sql.NewContext().PasswordResetTokens.Should().BeEmpty();
        BgEmail.DidNotReceive().QueuePasswordResetEmail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact] // UT-AUTH-RESET-03
    public async Task Forgot_inactive_user_does_not_send()
    {
        var u = AddUser(x => { x.IsEmailConfirmed = true; x.IsActive = false; });
        await Build().ForgotPasswordAsync(u.Email);

        Sql.NewContext().PasswordResetTokens.Should().BeEmpty();
    }

    [Fact] // UT-AUTH-RESET-04
    public async Task Forgot_valid_invalidates_old_and_queues_reset_link()
    {
        var u = AddUser(x => x.IsEmailConfirmed = true);
        var old = AddResetToken(u.UserId);

        var res = await Build().ForgotPasswordAsync(u.Email);

        res.Success.Should().BeTrue();
        using var read = Sql.NewContext();
        read.PasswordResetTokens.Single(x => x.Id == old.Id).IsUsed.Should().BeTrue();
        var fresh = read.PasswordResetTokens.Single(x => x.UserId == u.UserId && !x.IsUsed);
        fresh.ExpiredAt.Should().BeCloseTo(Now.AddHours(1), TimeSpan.FromMinutes(1));
        BgEmail.Received().QueuePasswordResetEmail(u.Email, u.FullName,
            Arg.Is<string>(link => link.Contains("/reset-password?token=")));
    }

    // ---- ResetPassword ----

    [Fact] // UT-AUTH-RESET-05
    public async Task Reset_unknown_token()
        => (await Build().ResetPasswordAsync("nope", "new-pw-123")).Message
            .Should().Be("Liên kết không hợp lệ hoặc đã hết hạn");

    [Fact] // UT-AUTH-RESET-06
    public async Task Reset_used_token()
    {
        var u = AddUser();
        var t = AddResetToken(u.UserId, x => x.IsUsed = true);
        (await Build().ResetPasswordAsync(t.Token, "new-pw-123")).Message
            .Should().Be("Liên kết không hợp lệ hoặc đã hết hạn");
    }

    [Fact] // UT-AUTH-RESET-07
    public async Task Reset_expired_token()
    {
        var u = AddUser();
        var t = AddResetToken(u.UserId, x => x.ExpiredAt = Now.AddMinutes(-1));
        (await Build().ResetPasswordAsync(t.Token, "new-pw-123")).Message
            .Should().Be("Liên kết không hợp lệ hoặc đã hết hạn");
    }

    [Fact] // UT-AUTH-RESET-08
    public async Task Reset_token_for_missing_user()
    {
        var u = AddUser();
        var t = AddResetToken(u.UserId);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(Arg.Any<int>()).Returns((User?)null);

        (await Build(users: users).ResetPasswordAsync(t.Token, "new-pw-123")).Message
            .Should().Be("Tài khoản không tồn tại");
    }

    [Fact] // UT-AUTH-RESET-09
    public async Task Reset_valid_rehashes_clears_lockout_and_kills_sessions()
    {
        var u = AddUser(x =>
        {
            x.FailedLoginCount = 4;
            x.LockoutEndsAt = Now.AddMinutes(10);
            x.PasswordHash = "OLD-HASH";
        });
        var oldStamp = u.SecurityStamp;
        var t = AddResetToken(u.UserId);

        var res = await Build().ResetPasswordAsync(t.Token, "new-pw-123");

        res.Success.Should().BeTrue();
        using var read = Sql.NewContext();
        var user = read.Users.Single(x => x.UserId == u.UserId);
        user.PasswordHash.Should().Be("hash:new-pw-123");
        user.FailedLoginCount.Should().Be(0);
        user.LockoutEndsAt.Should().BeNull();
        user.SecurityStamp.Should().NotBe(oldStamp);
        read.PasswordResetTokens.Single(x => x.Id == t.Id).IsUsed.Should().BeTrue();
        await RefreshTokens.Received().RevokeAllForUserAsync(u.UserId);
        Cache.Received().Remove($"sstamp:{u.UserId}");
    }

    [Fact] // UT-AUTH-RESET-10
    public async Task Reset_stores_a_hash_not_the_raw_password()
    {
        var u = AddUser(x => x.PasswordHash = "OLD-HASH");
        var t = AddResetToken(u.UserId);

        await Build().ResetPasswordAsync(t.Token, "plain-secret");

        Sql.NewContext().Users.Single(x => x.UserId == u.UserId).PasswordHash.Should().NotBe("plain-secret");
    }
}
