using ELearning_ToanHocHay_Control;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ELearning_ToanHocHay_Control.Tests.Infrastructure;

/// <summary>
/// Boots the real API against a real PostgreSQL (Testcontainers). Requires Docker;
/// without it, <see cref="DockerAvailable"/> is false and the tests are skipped.
/// </summary>
public class A1TestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestSecret = "test-secret-key-at-least-32-characters-long!!";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public bool DockerAvailable { get; private set; }
    public SeededIds Ids { get; private set; } = new();

    public async Task InitializeAsync()
    {
        try
        {
            await _db.StartAsync();
            DockerAvailable = true;
        }
        catch
        {
            DockerAvailable = false;
            return;
        }

        Environment.SetEnvironmentVariable("DATABASE_URL", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__MyCnn", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", TestSecret);
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "test-issuer");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "test-audience");
        Environment.SetEnvironmentVariable("JwtSettings__ExpirationMinutes", "60");

        // Touching Services builds the host, which runs Program.Main -> db.Database.Migrate().
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Ids = await TestSeed.SeedAsync(ctx);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production: disables the HTTPS redirect (307) so POST tests are not redirected.
        builder.UseEnvironment("Production");
    }

    /// <summary>Issues a real JWT for a seeded user (studentId / parentId resolved from the DB).</summary>
    public string CreateToken(int userId)
    {
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();

        var user = ctx.Users.Single(u => u.UserId == userId);
        int? studentId = ctx.Students.Where(s => s.UserId == userId)
            .Select(s => (int?)s.StudentId).FirstOrDefault();
        int? parentId = ctx.Parents.Where(p => p.UserId == userId)
            .Select(p => (int?)p.ParentId).FirstOrDefault();

        return jwt.GenerateToken(user, studentId, parentId);
    }

    public HttpClient ClientFor(int? userId)
    {
        var client = CreateClient();
        if (userId is int uid)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateToken(uid));
        return client;
    }

    /// <summary>Runs a read against a fresh <see cref="AppDbContext"/> scope.</summary>
    public async Task<T> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(ctx);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (DockerAvailable)
            await _db.DisposeAsync();
    }
}
