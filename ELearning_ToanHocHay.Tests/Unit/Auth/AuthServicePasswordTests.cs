using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.3 — UT-AUTH-PWD-* (logout + đổi mật khẩu).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class AuthServicePasswordTests
{
    private readonly AuthHarness _h = new();

    // ---- Logout ----

    [Fact] // UT-AUTH-PWD-01
    public async Task Logout_with_own_token_revokes_only_that_token()
    {
        var stored = new RefreshToken { UserId = 1, RevokedAt = null, ExpiresAt = _h.Now.AddDays(5) };
        _h.RefreshTokens.GetByHashAsync(Arg.Any<string>()).Returns(stored);

        var res = await _h.Build().LogoutAsync(1, "the-token");

        res.Success.Should().BeTrue();
        stored.RevokedAt.Should().Be(_h.Now);
        await _h.RefreshTokens.Received().SaveAsync();
        await _h.RefreshTokens.DidNotReceive().RevokeAllForUserAsync(Arg.Any<int>());
    }

    [Fact] // UT-AUTH-PWD-02
    public async Task Logout_without_token_revokes_everything()
    {
        var res = await _h.Build().LogoutAsync(1);

        res.Success.Should().BeTrue();
        await _h.RefreshTokens.Received().RevokeAllForUserAsync(1);
    }

    [Fact] // UT-AUTH-PWD-03
    public async Task Logout_with_another_users_token_revokes_nothing()
    {
        _h.RefreshTokens.GetByHashAsync(Arg.Any<string>())
            .Returns(new RefreshToken { UserId = 999, RevokedAt = null, ExpiresAt = _h.Now.AddDays(5) });

        var res = await _h.Build().LogoutAsync(1, "someone-elses");

        res.Success.Should().BeTrue();
        await _h.RefreshTokens.DidNotReceive().SaveAsync();
    }

    [Fact] // UT-AUTH-PWD-04
    public async Task Logout_with_already_revoked_token_is_a_noop_success()
    {
        var stored = new RefreshToken { UserId = 1, RevokedAt = _h.Now.AddDays(-1), ExpiresAt = _h.Now.AddDays(5) };
        _h.RefreshTokens.GetByHashAsync(Arg.Any<string>()).Returns(stored);

        var res = await _h.Build().LogoutAsync(1, "old-token");

        res.Success.Should().BeTrue();
        stored.RevokedAt.Should().Be(_h.Now.AddDays(-1));
        await _h.RefreshTokens.DidNotReceive().SaveAsync();
    }

    // ---- ChangePassword ----

    private static ChangePasswordDto Change() => new() { CurrentPassword = "old-pw", NewPassword = "new-pw-123" };

    [Fact] // UT-AUTH-PWD-05
    public async Task ChangePassword_unknown_user()
    {
        _h.Users.GetByIdAsync(1).Returns((User?)null);
        (await _h.Build().ChangePasswordAsync(1, Change())).Message.Should().Be("Tài khoản không tồn tại");
    }

    [Fact] // UT-AUTH-PWD-06
    public async Task ChangePassword_wrong_current_password_keeps_hash()
    {
        var user = Entities.NewUser(tweak: u => { u.UserId = 1; u.PasswordHash = "OLD-HASH"; });
        _h.Users.GetByIdAsync(1).Returns(user);
        _h.Hasher.VerifyPassword("old-pw", "OLD-HASH").Returns(false);

        var res = await _h.Build().ChangePasswordAsync(1, Change());

        res.Message.Should().Be("Mật khẩu hiện tại không đúng");
        user.PasswordHash.Should().Be("OLD-HASH");
    }

    [Fact] // UT-AUTH-PWD-07
    public async Task ChangePassword_success_rehashes_bumps_stamp_and_kills_sessions()
    {
        var user = Entities.NewUser(tweak: u => { u.UserId = 1; u.PasswordHash = "OLD-HASH"; });
        var oldStamp = user.SecurityStamp;
        _h.Users.GetByIdAsync(1).Returns(user);
        _h.Hasher.VerifyPassword("old-pw", "OLD-HASH").Returns(true);
        _h.Hasher.HashPassword("new-pw-123").Returns("NEW-HASH");

        var res = await _h.Build().ChangePasswordAsync(1, Change());

        res.Success.Should().BeTrue();
        user.PasswordHash.Should().Be("NEW-HASH");
        user.SecurityStamp.Should().NotBe(oldStamp);
        await _h.RefreshTokens.Received().RevokeAllForUserAsync(1);
        _h.Cache.Received().Remove("sstamp:1");
    }

    [Fact] // UT-AUTH-PWD-08
    public async Task ChangePassword_success_message_tells_user_to_reauthenticate()
    {
        var user = Entities.NewUser(tweak: u => { u.UserId = 1; u.PasswordHash = "OLD-HASH"; });
        _h.Users.GetByIdAsync(1).Returns(user);
        _h.Hasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _h.Hasher.HashPassword(Arg.Any<string>()).Returns("NEW-HASH");

        (await _h.Build().ChangePasswordAsync(1, Change())).Message.Should().Contain("đăng nhập lại");
    }
}
