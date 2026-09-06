using System.Security.Claims;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.7 — UT-AUTH-VALIDATE-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class AuthServiceValidateTokenTests
{
    private readonly AuthHarness _h = new();

    private void ArrangeValidJwt(int userId = 1)
    {
        _h.Jwt.ValidateToken(Arg.Any<string>()).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        _h.Jwt.GetUserIdFromToken(Arg.Any<string>()).Returns(userId);
    }

    [Fact] // UT-AUTH-VALIDATE-01
    public async Task Bad_signature_is_false()
    {
        _h.Jwt.ValidateToken(Arg.Any<string>()).Returns((ClaimsPrincipal?)null);
        (await _h.Build().ValidateTokenAsync("t")).Should().BeFalse();
    }

    [Fact] // UT-AUTH-VALIDATE-02
    public async Task No_user_id_in_token_is_false()
    {
        _h.Jwt.ValidateToken(Arg.Any<string>()).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        _h.Jwt.GetUserIdFromToken(Arg.Any<string>()).Returns((int?)null);
        (await _h.Build().ValidateTokenAsync("t")).Should().BeFalse();
    }

    [Fact] // UT-AUTH-VALIDATE-03
    public async Task Missing_user_is_false()
    {
        ArrangeValidJwt();
        _h.Users.GetByIdAsync(1).Returns((User?)null);
        (await _h.Build().ValidateTokenAsync("t")).Should().BeFalse();
    }

    [Theory] // UT-AUTH-VALIDATE-04
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Inactive_or_locked_user_is_false(bool isActive, bool locked)
    {
        ArrangeValidJwt();
        _h.Users.GetByIdAsync(1).Returns(Entities.NewUser(tweak: u =>
        {
            u.UserId = 1;
            u.IsActive = isActive;
            u.LockedAt = locked ? _h.Now : null;
        }));
        (await _h.Build().ValidateTokenAsync("t")).Should().BeFalse();
    }

    [Fact] // UT-AUTH-VALIDATE-05
    public async Task All_valid_is_true()
    {
        ArrangeValidJwt();
        _h.Users.GetByIdAsync(1).Returns(Entities.NewUser(tweak: u => { u.UserId = 1; u.IsActive = true; u.LockedAt = null; }));
        (await _h.Build().ValidateTokenAsync("t")).Should().BeTrue();
    }

    [Fact] // UT-AUTH-VALIDATE-06
    public async Task Jwt_exception_is_swallowed()
    {
        _h.Jwt.ValidateToken(Arg.Any<string>()).Throws(new Exception("kaboom"));
        (await _h.Build().ValidateTokenAsync("t")).Should().BeFalse();
    }
}
