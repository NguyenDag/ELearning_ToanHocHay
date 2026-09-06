using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>
/// Bộ đồ nghề cho các test U1 của <see cref="AuthService"/> (§3.1–3.3, 3.7): mọi phụ thuộc là
/// <c>NSubstitute</c>. <see cref="AppDbContext"/> chỉ là stub (Login/Refresh/Logout/ChangePassword/
/// ValidateToken không chạm tới nó sau refactor §2).
/// </summary>
internal sealed class AuthHarness
{
    public readonly IUserRepository Users = Substitute.For<IUserRepository>();
    public readonly IStudentRepository Students = Substitute.For<IStudentRepository>();
    public readonly IParentRepository Parents = Substitute.For<IParentRepository>();
    public readonly IRefreshTokenRepository RefreshTokens = Substitute.For<IRefreshTokenRepository>();
    public readonly IJwtService Jwt = Substitute.For<IJwtService>();
    public readonly IPasswordHasher Hasher = Substitute.For<IPasswordHasher>();
    public readonly IEmailService Email = Substitute.For<IEmailService>();
    public readonly IBackgroundEmailService BgEmail = Substitute.For<IBackgroundEmailService>();
    public readonly IMemoryCache Cache = Substitute.For<IMemoryCache>();
    public readonly IPackageTierResolver TierResolver = Substitute.For<IPackageTierResolver>();
    public readonly IRefreshTokenIssuer Issuer = Substitute.For<IRefreshTokenIssuer>();
    public readonly FakeClock Clock = new(new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero));

    public DateTime Now => Clock.GetUtcNow().UtcDateTime;

    /// <summary>Cặp token mà <see cref="Issuer"/> trả về mặc định.</summary>
    public TokenPairDto IssuedPair { get; }

    public AuthHarness()
    {
        IssuedPair = new TokenPairDto
        {
            Token = "access-token",
            RefreshToken = "new-refresh-token",
            TokenExpiration = Now.AddMinutes(30),
            RefreshTokenExpiration = Now.AddDays(30),
        };
        Issuer.IssueAsync(Arg.Any<User>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>())
            .Returns(IssuedPair);
        TierResolver.ResolveAsync(Arg.Any<int>()).Returns(PackageTier.Free);
    }

    private static AppDbContext StubDb() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options);

    public AuthService Build() => new(
        StubDb(), Users, Students, Parents, RefreshTokens, Jwt, Hasher, Email,
        Options.Create(new AppSettings { BaseUrl = "https://webapp.test" }),
        BgEmail, Cache, TierResolver, Issuer, Clock);
}
