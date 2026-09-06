using ELearning_ToanHocHay_Control.Data.Entities;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.4 — UT-AUTH-CONFIRM-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class AuthServiceConfirmEmailTests : AuthU2Base
{
    private EmailVerificationToken AddToken(int userId, Action<EmailVerificationToken>? tweak = null)
    {
        var token = new EmailVerificationToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString("N"),
            ExpiredAt = Now.AddHours(24),
            IsUsed = false,
        };
        tweak?.Invoke(token);
        Sql.Db.EmailVerificationTokens.Add(token);
        Sql.Db.SaveChanges();
        return token;
    }

    [Fact] // UT-AUTH-CONFIRM-01
    public async Task Unknown_token()
        => (await Build().ConfirmEmailAsync("nope")).Message.Should().Be("Liên kết không hợp lệ");

    [Fact] // UT-AUTH-CONFIRM-02
    public async Task Used_token()
    {
        var u = AddUser(x => x.IsEmailConfirmed = false);
        var t = AddToken(u.UserId, x => x.IsUsed = true);
        (await Build().ConfirmEmailAsync(t.Token)).Message.Should().Be("Liên kết không hợp lệ");
    }

    [Fact] // UT-AUTH-CONFIRM-03
    public async Task Expired_token()
    {
        var u = AddUser(x => x.IsEmailConfirmed = false);
        var t = AddToken(u.UserId, x => x.ExpiredAt = Now.AddMinutes(-1));
        (await Build().ConfirmEmailAsync(t.Token)).Message.Should().Be("Liên kết không hợp lệ");
    }

    [Fact] // UT-AUTH-CONFIRM-04
    public async Task Valid_token_confirms_the_user()
    {
        var u = AddUser(x => { x.IsEmailConfirmed = false; x.EmailConfirmedAt = null; });
        var t = AddToken(u.UserId);

        var res = await Build().ConfirmEmailAsync(t.Token);

        res.Success.Should().BeTrue();
        using var read = Sql.NewContext();
        var user = read.Users.Single(x => x.UserId == u.UserId);
        user.IsEmailConfirmed.Should().BeTrue();
        user.EmailConfirmedAt.Should().NotBeNull();
        read.EmailVerificationTokens.Single(x => x.Id == t.Id).IsUsed.Should().BeTrue();
    }

    [Fact] // UT-AUTH-CONFIRM-05
    public async Task Resend_unknown_email_is_fuzzy_success_without_queue()
    {
        var res = await Build().ResendConfirmationEmailAsync("ghost@test.local");

        res.Success.Should().BeTrue();
        res.Message.Should().Contain("Nếu email tồn tại");
        BgEmail.DidNotReceive().QueueConfirmationEmail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact] // UT-AUTH-CONFIRM-06
    public async Task Resend_already_confirmed_account()
    {
        var u = AddUser(x => x.IsEmailConfirmed = true);
        var res = await Build().ResendConfirmationEmailAsync(u.Email);

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Tài khoản này đã được xác nhận trước đó");
    }

    [Fact] // UT-AUTH-CONFIRM-07
    public async Task Resend_valid_invalidates_old_tokens_and_queues_new_link()
    {
        var u = AddUser(x => x.IsEmailConfirmed = false);
        var old = AddToken(u.UserId);

        var res = await Build().ResendConfirmationEmailAsync(u.Email);

        res.Success.Should().BeTrue();
        using var read = Sql.NewContext();
        read.EmailVerificationTokens.Single(x => x.Id == old.Id).IsUsed.Should().BeTrue();
        var fresh = read.EmailVerificationTokens.Single(x => x.UserId == u.UserId && !x.IsUsed);
        fresh.ExpiredAt.Should().BeCloseTo(Now.AddHours(24), TimeSpan.FromMinutes(1));
        BgEmail.Received().QueueConfirmationEmail(u.Email, u.FullName,
            Arg.Is<string>(link => link.Contains("/api/auth/confirm-email?token=")));
    }

    [Fact] // UT-AUTH-CONFIRM-08
    public async Task Resend_link_has_no_double_slash_from_base_url()
    {
        BaseUrl = "https://webapp.test/";
        var u = AddUser(x => x.IsEmailConfirmed = false);

        await Build().ResendConfirmationEmailAsync(u.Email);

        BgEmail.Received().QueueConfirmationEmail(Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string>(link => !link.Replace("https://", "").Contains("//")));
    }
}
