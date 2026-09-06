using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Implementations;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Auth;

/// <summary>
/// Nền tầng <b>U2</b> cho <see cref="AuthService"/> (§3.4–3.6): <see cref="AppDbContext"/> SQLite
/// thật + repo thật (<c>UserRepository</c>…) trên cùng context; email / hasher / cache / refresh
/// token repo là fake. <see cref="FakeClock"/> cố định để khẳng định "≈ now + 24h / 1h".
/// </summary>
public abstract class AuthU2Base : IDisposable
{
    protected readonly SqliteDb Sql = SqliteDb.New();
    protected readonly FakeClock Clock = new(new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero));
    protected readonly IPasswordHasher Hasher = Substitute.For<IPasswordHasher>();
    protected readonly IBackgroundEmailService BgEmail = Substitute.For<IBackgroundEmailService>();
    protected readonly IMemoryCache Cache = Substitute.For<IMemoryCache>();
    protected readonly IRefreshTokenRepository RefreshTokens = Substitute.For<IRefreshTokenRepository>();
    protected string BaseUrl = "https://webapp.test";

    protected AuthU2Base()
    {
        Hasher.HashPassword(Arg.Any<string>()).Returns(ci => "hash:" + ci.Arg<string>());
        Hasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
    }

    protected DateTime Now => Clock.GetUtcNow().UtcDateTime;

    public void Dispose() => Sql.Dispose();

    protected AuthService Build(
        IUserRepository? users = null,
        IStudentRepository? students = null,
        IParentRepository? parents = null)
        => new(
            Sql.Db,
            users ?? new UserRepository(Sql.Db),
            students ?? new StudentRepository(Sql.Db),
            parents ?? new ParentRepository(Sql.Db),
            RefreshTokens,
            Substitute.For<IJwtService>(),
            Hasher,
            Substitute.For<IEmailService>(),
            Options.Create(new AppSettings { BaseUrl = BaseUrl }),
            BgEmail,
            Cache,
            Substitute.For<IPackageTierResolver>(),
            Substitute.For<IRefreshTokenIssuer>(),
            Clock);

    protected User AddUser(Action<User>? tweak = null)
    {
        var user = Entities.NewUser(tweak: tweak);
        Sql.Db.Users.Add(user);
        Sql.Db.SaveChanges();
        return user;
    }
}
