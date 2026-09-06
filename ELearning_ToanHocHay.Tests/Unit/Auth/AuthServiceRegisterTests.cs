using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.6 — UT-AUTH-REGISTER-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class AuthServiceRegisterTests : AuthU2Base
{
    private static RegisterRequestDto Req(UserType type = UserType.Student, string email = "new@test.local", Action<RegisterRequestDto>? tweak = null)
    {
        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = "secret123",
            ConfirmPassword = "secret123",
            FullName = "Người Mới",
            UserType = type,
            GradeLevelId = 1,
            SchoolName = "THCS Test",
            Job = "Kỹ sư",
        };
        tweak?.Invoke(dto);
        return dto;
    }

    [Fact] // UT-AUTH-REGISTER-01
    public async Task Existing_unconfirmed_email()
    {
        AddUser(u => { u.Email = "dup@test.local"; u.IsEmailConfirmed = false; });
        (await Build().RegisterAsync(Req(email: "dup@test.local"))).Message
            .Should().Be("Email đã được đăng ký và chờ xác nhận");
    }

    [Fact] // UT-AUTH-REGISTER-02
    public async Task Existing_confirmed_email()
    {
        AddUser(u => { u.Email = "dup@test.local"; u.IsEmailConfirmed = true; });
        (await Build().RegisterAsync(Req(email: "dup@test.local"))).Message
            .Should().Be("Email đã được đăng ký");
    }

    [Theory] // UT-AUTH-REGISTER-03
    [InlineData(UserType.ContentEditor)]
    [InlineData(UserType.AcademicReviewer)]
    [InlineData(UserType.FinanceManager)]
    [InlineData(UserType.SystemAdmin)]
    public async Task Privileged_roles_cannot_self_register_and_roll_back(UserType type)
    {
        var res = await Build().RegisterAsync(Req(type: type));

        res.Message.Should().Be("Không cho phép đăng ký role này");
        Sql.NewContext().Users.Should().BeEmpty();
    }

    [Fact] // UT-AUTH-REGISTER-04
    public async Task Student_registration_creates_full_graph_and_queues_email()
    {
        var res = await Build().RegisterAsync(Req(type: UserType.Student));

        res.Success.Should().BeTrue();
        using var read = Sql.NewContext();
        var user = read.Users.Single();
        var student = read.Students.Single(s => s.UserId == user.UserId);
        student.CurrentGradeLevelId.Should().Be(1);
        student.SchoolName.Should().Be("THCS Test");
        var token = read.EmailVerificationTokens.Single(t => t.UserId == user.UserId);
        token.ExpiredAt.Should().BeCloseTo(Now.AddHours(24), TimeSpan.FromMinutes(1));
        BgEmail.Received().QueueConfirmationEmail(user.Email, user.FullName, Arg.Any<string>());
    }

    [Fact] // UT-AUTH-REGISTER-05
    public async Task Parent_registration_makes_an_8_char_uppercase_connection_code()
    {
        var res = await Build().RegisterAsync(Req(type: UserType.Parent, email: "parent@test.local"));

        res.Success.Should().BeTrue();
        var parent = Sql.NewContext().Parents.Single();
        parent.Job.Should().Be("Kỹ sư");
        parent.ConnectionCode.Should().HaveLength(8).And.Be(parent.ConnectionCode.ToUpperInvariant());
    }

    [Fact] // UT-AUTH-REGISTER-06
    public async Task Password_is_hashed()
    {
        await Build().RegisterAsync(Req());
        Sql.NewContext().Users.Single().PasswordHash.Should().NotBe("secret123");
    }

    [Fact] // UT-AUTH-REGISTER-07
    public async Task Failure_midway_rolls_back_and_hides_internals()
    {
        var students = Substitute.For<IStudentRepository>();
        students.AddAsync(Arg.Any<Student>()).ThrowsAsync(new Exception("db exploded"));

        var res = await Build(students: students).RegisterAsync(Req(type: UserType.Student));

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Đăng ký thất bại, vui lòng thử lại sau");
        res.Message.Should().NotContain("db exploded");
        Sql.NewContext().Users.Should().BeEmpty();
    }

    [Fact] // UT-AUTH-REGISTER-08
    public async Task Confirmation_email_is_queued_only_after_a_committed_registration()
    {
        await Build().RegisterAsync(Req(type: UserType.Student));

        // committed: the user survives in a fresh context …
        Sql.NewContext().Users.Should().ContainSingle();
        // … and the email went out
        BgEmail.Received(1).QueueConfirmationEmail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
