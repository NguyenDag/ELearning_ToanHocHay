using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.2 — UT-AUTH-REFRESH-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class AuthServiceRefreshTokenTests
{
    private readonly AuthHarness _h = new();

    private RefreshToken ArrangeActiveToken(int userId = 1, Action<RefreshToken>? tweak = null)
    {
        var token = new RefreshToken
        {
            RefreshTokenId = 1,
            UserId = userId,
            TokenHash = "stored-hash",
            RevokedAt = null,
            ExpiresAt = _h.Now.AddDays(5),
        };
        tweak?.Invoke(token);
        _h.RefreshTokens.GetByHashAsync(Arg.Any<string>()).Returns(token);
        return token;
    }

    private User ArrangeUser(int userId = 1, UserType type = UserType.SystemAdmin, Action<User>? tweak = null)
    {
        var user = Entities.NewUser(type: type, tweak: u => u.UserId = userId);
        tweak?.Invoke(user);
        _h.Users.GetByIdAsync(userId).Returns(user);
        return user;
    }

    [Theory] // UT-AUTH-REFRESH-01
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Blank_token_is_invalid(string? token)
        => (await _h.Build().RefreshTokenAsync(token!)).Message.Should().Be("Refresh token không hợp lệ");

    [Fact] // UT-AUTH-REFRESH-02
    public async Task Unknown_hash_is_invalid()
    {
        _h.RefreshTokens.GetByHashAsync(Arg.Any<string>()).Returns((RefreshToken?)null);
        (await _h.Build().RefreshTokenAsync("whatever")).Message.Should().Be("Refresh token không hợp lệ");
    }

    [Fact] // UT-AUTH-REFRESH-03
    public async Task Reused_revoked_token_cuts_every_session()
    {
        ArrangeActiveToken(userId: 42, tweak: t => t.RevokedAt = _h.Now.AddMinutes(-1));

        var res = await _h.Build().RefreshTokenAsync("x");

        res.Success.Should().BeFalse();
        await _h.RefreshTokens.Received().RevokeAllForUserAsync(42);
    }

    [Fact] // UT-AUTH-REFRESH-04
    public async Task Expired_token_does_not_cut_sessions()
    {
        ArrangeActiveToken(tweak: t => t.ExpiresAt = _h.Now.AddMinutes(-1));

        var res = await _h.Build().RefreshTokenAsync("x");

        res.Success.Should().BeFalse();
        await _h.RefreshTokens.DidNotReceive().RevokeAllForUserAsync(Arg.Any<int>());
    }

    [Fact] // UT-AUTH-REFRESH-05
    public async Task Locked_user_cannot_refresh()
    {
        ArrangeActiveToken();
        ArrangeUser(tweak: u => u.IsActive = false);

        (await _h.Build().RefreshTokenAsync("x")).Message.Should().Be("Tài khoản không tồn tại hoặc bị khóa");
    }

    [Fact] // UT-AUTH-REFRESH-06
    public async Task Valid_refresh_rotates_the_token()
    {
        var stored = ArrangeActiveToken();
        ArrangeUser();

        var res = await _h.Build().RefreshTokenAsync("x");

        res.Success.Should().BeTrue();
        stored.RevokedAt.Should().Be(_h.Now);
        stored.ReplacedByTokenHash.Should().Be(SecureTokens.Hash(_h.IssuedPair.RefreshToken));
        await _h.RefreshTokens.Received().SaveAsync();
    }

    [Fact] // UT-AUTH-REFRESH-07
    public async Task Student_token_is_issued_with_student_id()
    {
        ArrangeActiveToken();
        ArrangeUser(type: UserType.Student);
        _h.Students.GetByUserIdAsync(1).Returns(new Student { StudentId = 9, UserId = 1 });

        await _h.Build().RefreshTokenAsync("x");

        await _h.Issuer.Received().IssueAsync(Arg.Any<User>(), 9, null, Arg.Any<string?>());
    }

    [Fact] // UT-AUTH-REFRESH-08
    public async Task Parent_token_is_issued_with_parent_id()
    {
        ArrangeActiveToken();
        ArrangeUser(type: UserType.Parent);
        _h.Parents.GetByUserIdAsync(1).Returns(new Parent { ParentId = 3, UserId = 1 });

        await _h.Build().RefreshTokenAsync("x");

        await _h.Issuer.Received().IssueAsync(Arg.Any<User>(), null, 3, Arg.Any<string?>());
    }

    [Fact] // UT-AUTH-REFRESH-09
    public async Task Ip_is_forwarded_to_the_issuer()
    {
        ArrangeActiveToken();
        ArrangeUser();

        await _h.Build().RefreshTokenAsync("x", ip: "9.9.9.9");

        await _h.Issuer.Received().IssueAsync(Arg.Any<User>(), Arg.Any<int?>(), Arg.Any<int?>(), "9.9.9.9");
    }
}
