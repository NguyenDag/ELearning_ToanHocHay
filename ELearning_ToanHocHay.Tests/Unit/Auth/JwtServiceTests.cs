using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.9 — UT-JWT-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class JwtServiceTests
{
    private static JwtService Svc(IConfiguration? cfg = null) => new(cfg ?? TestConfig.Jwt());

    private static User User(int id = 1, UserType type = UserType.Student, string? stamp = "stamp-1")
        => Entities.NewUser(email: "u@test.local", type: type, tweak: u => { u.UserId = id; u.SecurityStamp = stamp!; });

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact] // UT-JWT-01
    public void GenerateToken_throws_without_secret()
    {
        var svc = Svc(TestConfig.Jwt(("JwtSettings:SecretKey", "")));
        var act = () => svc.GenerateToken(User());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact] // UT-JWT-02
    public void GenerateToken_student_carries_student_id_only()
    {
        var jwt = Read(Svc().GenerateToken(User(7, UserType.Student), studentId: 7, parentId: null));

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role);
        jwt.Claims.Should().Contain(c => c.Type == CustomJwtClaims.UserId);
        jwt.Claims.Should().Contain(c => c.Type == CustomJwtClaims.UserType);
        jwt.Claims.Should().Contain(c => c.Type == CustomJwtClaims.SecurityStamp);
        jwt.Claims.Should().Contain(c => c.Type == CustomJwtClaims.StudentId && c.Value == "7");
        jwt.Claims.Should().NotContain(c => c.Type == CustomJwtClaims.ParentId);
    }

    [Fact] // UT-JWT-03
    public void GenerateToken_parent_carries_parent_id_only()
    {
        var jwt = Read(Svc().GenerateToken(User(1, UserType.Parent), parentId: 9));

        jwt.Claims.Should().Contain(c => c.Type == CustomJwtClaims.ParentId && c.Value == "9");
        jwt.Claims.Should().NotContain(c => c.Type == CustomJwtClaims.StudentId);
    }

    [Fact] // UT-JWT-04
    public void GenerateToken_null_security_stamp_becomes_empty_claim()
    {
        var jwt = Read(Svc().GenerateToken(User(1, stamp: null)));
        jwt.Claims.Single(c => c.Type == CustomJwtClaims.SecurityStamp).Value.Should().Be("");
    }

    [Fact] // UT-JWT-05
    public void GenerateToken_role_claim_is_user_type()
    {
        var jwt = Read(Svc().GenerateToken(User(1, UserType.SystemAdmin)));
        jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value.Should().Be("SystemAdmin");
    }

    [Fact] // UT-JWT-06
    public void ValidateToken_accepts_own_token()
    {
        var svc = Svc();
        svc.ValidateToken(svc.GenerateToken(User())).Should().NotBeNull();
    }

    [Fact] // UT-JWT-07
    public void ValidateToken_rejects_foreign_signature()
    {
        var issued = Svc(TestConfig.Jwt(("JwtSettings:SecretKey", "another-signing-key-that-is-also-32+chars"))).GenerateToken(User());
        Svc().ValidateToken(issued).Should().BeNull();
    }

    [Fact] // UT-JWT-08
    public void ValidateToken_rejects_expired()
    {
        var svc = Svc(TestConfig.Jwt(("JwtSettings:ExpirationMinutes", "-5")));
        svc.ValidateToken(svc.GenerateToken(User())).Should().BeNull();
    }

    [Fact] // UT-JWT-09
    public void ValidateToken_rejects_wrong_issuer()
    {
        var issued = Svc(TestConfig.Jwt(("JwtSettings:Issuer", "someone-else"))).GenerateToken(User());
        Svc().ValidateToken(issued).Should().BeNull();
    }

    [Fact] // UT-JWT-10
    public void ValidateToken_rejects_wrong_audience()
    {
        var issued = Svc(TestConfig.Jwt(("JwtSettings:Audience", "someone-else"))).GenerateToken(User());
        Svc().ValidateToken(issued).Should().BeNull();
    }

    [Fact] // UT-JWT-11
    public void ValidateToken_garbage_returns_null()
        => Svc().ValidateToken("abc.def").Should().BeNull();

    [Fact] // UT-JWT-12
    public void GetUserIdFromToken_valid()
    {
        var svc = Svc();
        svc.GetUserIdFromToken(svc.GenerateToken(User(123))).Should().Be(123);
    }

    [Fact] // UT-JWT-13
    public void GetUserIdFromToken_garbage_is_null()
        => Svc().GetUserIdFromToken("not-a-token").Should().BeNull();

    [Fact] // UT-JWT-14
    public void GenerateToken_unparseable_expiration_defaults_to_60_minutes()
    {
        var svc = Svc(TestConfig.Jwt(("JwtSettings:ExpirationMinutes", "abc")));
        var jwt = Read(svc.GenerateToken(User()));
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(2));
    }
}
