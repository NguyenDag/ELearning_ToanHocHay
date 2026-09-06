using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.1 — UT-AUTH-LOGIN-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class AuthServiceLoginTests
{
    private readonly AuthHarness _h = new();

    private static LoginRequestDto Login(string email = "u@test.local", string pwd = "pw")
        => new() { Email = email, Password = pwd };

    private User ArrangeUser(Action<User>? tweak = null)
    {
        var user = Entities.NewUser(email: "u@test.local", type: UserType.SystemAdmin, tweak: u => u.UserId = 1);
        tweak?.Invoke(user);
        _h.Users.GetByEmailAsync("u@test.local").Returns(user);
        _h.Hasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        return user;
    }

    [Fact] // UT-AUTH-LOGIN-01
    public async Task Unknown_email_fails_without_checking_password()
    {
        _h.Users.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Email hoặc mật khẩu không đúng");
        _h.Hasher.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact] // UT-AUTH-LOGIN-02
    public async Task Wrong_password_increments_failure_counter()
    {
        var user = ArrangeUser();
        _h.Hasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Email hoặc mật khẩu không đúng");
        user.FailedLoginCount.Should().Be(1);
        await _h.Users.Received().UpdateUserAsync(user);
    }

    [Fact] // UT-AUTH-LOGIN-03
    public async Task Fifth_wrong_password_sets_one_minute_lockout()
    {
        var user = ArrangeUser(u => u.FailedLoginCount = 4);
        _h.Hasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await _h.Build().LoginAsync(Login());

        user.FailedLoginCount.Should().Be(5);
        user.LockoutEndsAt.Should().Be(_h.Now.AddMinutes(1));
    }

    [Fact] // UT-AUTH-LOGIN-04
    public async Task Active_lockout_blocks_without_password_check()
    {
        ArrangeUser(u => u.LockoutEndsAt = _h.Now.AddMinutes(10));

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("tạm khoá").And.Contain("Thử lại sau");
        _h.Hasher.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact] // UT-AUTH-LOGIN-05
    public async Task Expired_lockout_plus_right_password_logs_in_and_clears_counter()
    {
        var user = ArrangeUser(u =>
        {
            u.FailedLoginCount = 6;
            u.LockoutEndsAt = _h.Now.AddMinutes(-1);
        });

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeTrue();
        user.FailedLoginCount.Should().Be(0);
        user.LockoutEndsAt.Should().BeNull();
    }

    [Fact] // UT-AUTH-LOGIN-06
    public async Task Unconfirmed_email_is_rejected()
    {
        ArrangeUser(u => { u.IsEmailConfirmed = false; u.EmailConfirmedAt = null; });

        var res = await _h.Build().LoginAsync(Login());

        res.Message.Should().Be("Vui lòng xác nhận email trước khi đăng nhập");
    }

    [Fact] // UT-AUTH-LOGIN-07
    public async Task Inactive_account_is_rejected()
    {
        ArrangeUser(u => u.IsActive = false);
        (await _h.Build().LoginAsync(Login())).Message.Should().Be("Tài khoản đã bị vô hiệu hóa");
    }

    [Fact] // UT-AUTH-LOGIN-08
    public async Task Admin_locked_account_is_rejected()
    {
        ArrangeUser(u => u.LockedAt = _h.Now);
        (await _h.Build().LoginAsync(Login())).Message.Should().Be("Tài khoản đã bị vô hiệu hóa");
    }

    [Fact] // UT-AUTH-LOGIN-09
    public async Task Student_without_student_record_is_rejected()
    {
        ArrangeUser(u => u.UserType = UserType.Student);
        _h.Students.GetByUserIdAsync(Arg.Any<int>()).Returns((Student?)null);

        (await _h.Build().LoginAsync(Login())).Message.Should().Be("Không tìm thấy thông tin học sinh");
    }

    [Fact] // UT-AUTH-LOGIN-10
    public async Task Parent_without_parent_record_is_rejected()
    {
        ArrangeUser(u => u.UserType = UserType.Parent);
        _h.Parents.GetByUserIdAsync(Arg.Any<int>()).Returns((Parent?)null);

        (await _h.Build().LoginAsync(Login())).Message.Should().Be("Không tìm thấy thông tin phụ huynh");
    }

    [Theory] // UT-AUTH-LOGIN-11 / 12
    [InlineData(PackageTier.Premium)]
    [InlineData(PackageTier.Free)]
    public async Task Student_login_carries_resolved_tier_and_student_id(PackageTier tier)
    {
        ArrangeUser(u => u.UserType = UserType.Student);
        _h.Students.GetByUserIdAsync(1).Returns(new Student { StudentId = 77, UserId = 1 });
        _h.TierResolver.ResolveAsync(77).Returns(tier);

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeTrue();
        res.Data.PackageTier.Should().Be(tier);
        res.Data.StudentId.Should().Be(77);
    }

    [Fact] // UT-AUTH-LOGIN-13
    public async Task Successful_login_returns_token_pair_and_stamps_last_login()
    {
        ArrangeUser();

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeTrue();
        res.Data.Token.Should().NotBeNullOrEmpty();
        res.Data.RefreshToken.Should().NotBeNullOrEmpty();
        await _h.Users.Received().UpdateLastLoginAsync(1);
        await _h.Issuer.Received(1).IssueAsync(Arg.Any<User>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>());
    }

    [Fact] // UT-AUTH-LOGIN-14
    public async Task Counter_is_reset_on_success_after_earlier_failures()
    {
        var user = ArrangeUser(u => u.FailedLoginCount = 3);

        await _h.Build().LoginAsync(Login());

        user.FailedLoginCount.Should().Be(0);
    }

    [Fact] // UT-AUTH-LOGIN-15
    public async Task Repository_exception_is_swallowed()
    {
        _h.Users.GetByEmailAsync(Arg.Any<string>()).ThrowsAsync(new InvalidOperationException("boom"));

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Đã xảy ra lỗi");
    }

    [Fact] // UT-AUTH-LOGIN-16
    public async Task Parent_login_carries_parent_id_only()
    {
        ArrangeUser(u => u.UserType = UserType.Parent);
        _h.Parents.GetByUserIdAsync(1).Returns(new Parent { ParentId = 5, UserId = 1 });

        var res = await _h.Build().LoginAsync(Login());

        res.Success.Should().BeTrue();
        res.Data.ParentId.Should().Be(5);
        res.Data.StudentId.Should().BeNull();
        res.Data.PackageTier.Should().Be(PackageTier.Free);
    }

    [Fact] // UT-AUTH-LOGIN-17
    public async Task Ip_is_forwarded_to_the_token_issuer()
    {
        ArrangeUser();

        await _h.Build().LoginAsync(Login(), ip: "1.2.3.4");

        await _h.Issuer.Received().IssueAsync(Arg.Any<User>(), Arg.Any<int?>(), Arg.Any<int?>(), "1.2.3.4");
    }
}
