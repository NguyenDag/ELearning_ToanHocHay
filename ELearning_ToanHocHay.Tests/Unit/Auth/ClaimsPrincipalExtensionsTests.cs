using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.12 — UT-CLAIMS-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal P(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact] // UT-CLAIMS-01
    public void GetUserId_from_custom_claim()
        => P(new Claim(CustomJwtClaims.UserId, "42")).GetUserId().Should().Be(42);

    [Fact] // UT-CLAIMS-02
    public void GetUserId_falls_back_to_name_identifier()
        => P(new Claim(ClaimTypes.NameIdentifier, "42")).GetUserId().Should().Be(42);

    [Fact] // UT-CLAIMS-03
    public void GetUserId_missing_is_null()
        => P().GetUserId().Should().BeNull();

    [Fact] // UT-CLAIMS-04
    public void GetUserId_non_numeric_is_null()
        => P(new Claim(CustomJwtClaims.UserId, "abc")).GetUserId().Should().BeNull();

    [Fact] // UT-CLAIMS-05
    public void GetStudentId_present_and_absent()
    {
        P(new Claim(CustomJwtClaims.StudentId, "7")).GetStudentId().Should().Be(7);
        P().GetStudentId().Should().BeNull();
    }

    [Fact] // UT-CLAIMS-06
    public void GetParentId_present_and_absent()
    {
        P(new Claim(CustomJwtClaims.ParentId, "9")).GetParentId().Should().Be(9);
        P().GetParentId().Should().BeNull();
    }

    [Fact] // UT-CLAIMS-07
    public void GetUserType_valid()
        => P(new Claim(CustomJwtClaims.UserType, "SystemAdmin")).GetUserType().Should().Be(UserType.SystemAdmin);

    [Fact] // UT-CLAIMS-08
    public void GetUserType_invalid_is_null()
        => P(new Claim(CustomJwtClaims.UserType, "NotAType")).GetUserType().Should().BeNull();

    [Fact] // UT-CLAIMS-09
    public void GetEmail_from_either_claim_type()
    {
        P(new Claim(ClaimTypes.Email, "a@test.local")).GetEmail().Should().Be("a@test.local");
        P(new Claim(JwtRegisteredClaimNames.Email, "b@test.local")).GetEmail().Should().Be("b@test.local");
    }

    [Fact] // UT-CLAIMS-10
    public void HasUserType_matches()
        => P(new Claim(CustomJwtClaims.UserType, "SystemAdmin"))
            .HasUserType(UserType.ContentEditor, UserType.SystemAdmin).Should().BeTrue();

    [Fact] // UT-CLAIMS-11
    public void HasUserType_no_match()
        => P(new Claim(CustomJwtClaims.UserType, "Student"))
            .HasUserType(UserType.ContentEditor, UserType.SystemAdmin).Should().BeFalse();

    [Fact] // UT-CLAIMS-12
    public void IsSystemAdmin()
    {
        P(new Claim(CustomJwtClaims.UserType, "SystemAdmin")).IsSystemAdmin().Should().BeTrue();
        P(new Claim(CustomJwtClaims.UserType, "Student")).IsSystemAdmin().Should().BeFalse();
    }

    [Fact] // UT-CLAIMS-13
    public void Null_principal_is_safe()
    {
        ClaimsPrincipal? nil = null;
        nil.GetUserId().Should().BeNull();
        nil.GetStudentId().Should().BeNull();
        nil.GetParentId().Should().BeNull();
        nil.GetUserType().Should().BeNull();
        nil.GetEmail().Should().BeNull();
        nil.HasUserType(UserType.SystemAdmin).Should().BeFalse();
        nil.IsSystemAdmin().Should().BeFalse();
    }
}
