using ELearning_ToanHocHay_Control.Services.Implementations;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>§3.10 — UT-PWD-*. Bọc BCrypt.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact] // UT-PWD-01
    public void HashPassword_uses_random_salt()
        => _hasher.HashPassword("same-password").Should().NotBe(_hasher.HashPassword("same-password"));

    [Fact] // UT-PWD-02
    public void VerifyPassword_matches_own_hash()
        => _hasher.VerifyPassword("correct-horse", _hasher.HashPassword("correct-horse")).Should().BeTrue();

    [Fact] // UT-PWD-03
    public void VerifyPassword_rejects_wrong_password()
        => _hasher.VerifyPassword("sai", _hasher.HashPassword("dung")).Should().BeFalse();

    [Fact] // UT-PWD-04
    public void VerifyPassword_non_bcrypt_hash_is_false()
        => _hasher.VerifyPassword("pwd", "khong-phai-bcrypt").Should().BeFalse();

    [Theory] // UT-PWD-05
    [InlineData("")]
    [InlineData(null)]
    public void VerifyPassword_empty_or_null_hash_is_false(string? hash)
        => _hasher.VerifyPassword("pwd", hash!).Should().BeFalse();

    [Fact] // UT-PWD-06
    public void HashPassword_supports_unicode()
    {
        const string pwd = "Mật-khẩu-Ünïcödé-🎉";
        _hasher.VerifyPassword(pwd, _hasher.HashPassword(pwd)).Should().BeTrue();
    }
}
