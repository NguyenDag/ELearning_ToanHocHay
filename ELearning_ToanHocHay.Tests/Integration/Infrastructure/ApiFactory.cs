using ELearning_ToanHocHay_Control;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> boot API thật trên PostgreSQL thật
/// (Testcontainers). Máy không có Docker ⇒ <see cref="DockerAvailable"/> = false và mọi test
/// tự skip (<c>RequireDocker()</c>).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string JwtKey = "integration-test-signing-key-0123456789abcdef";
    private const string SePayKey = "test-sepay-key";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public bool DockerAvailable { get; private set; }
    public SeededIds Ids { get; private set; } = default!;

    public FakeAiService Ai => Services.GetRequiredService<FakeAiService>();
    public FakeEmailSink Email => Services.GetRequiredService<FakeEmailSink>();

    // ---------------------------------------------------------------- lifetime

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DATABASE_URL", null);
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", JwtKey);
        Environment.SetEnvironmentVariable("APP_BASE_URL", "https://webapp.test");

        try
        {
            await _container.StartAsync();
            DockerAvailable = true;
        }
        catch
        {
            DockerAvailable = false;
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Ids = await SeedData.EnsureAsync(db);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    // ---------------------------------------------------------------- host config

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:MyCnn", _container.GetConnectionString());
        builder.UseSetting("JwtSettings:SecretKey", JwtKey);
        builder.UseSetting("SePay:ApiKeyValidator", SePayKey);
        builder.UseSetting("SePay:LifecycleIntervalMinutes", "0");   // tắt hosted timer
        builder.UseSetting("SePay:AmountToleranceVnd", "0");
        builder.UseSetting("APP_BASE_URL", "https://webapp.test");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "1000000");
        builder.UseSetting("RateLimiting:RefundPermitLimit", "1000000");

        builder.ConfigureTestServices(services =>
        {
            // Tắt hosted service (BackgroundEmail cast lỗi vì đã swap; lifecycle timer),
            // nhưng GIỮ vòng drain AI feedback để F9-05 chạy được (FakeAiService trả ngay).
            services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
            services.AddHostedService(sp =>
                (ELearning_ToanHocHay_Control.Services.Implementations.AiFeedbackBackgroundService)
                    sp.GetRequiredService<IAiFeedbackQueue>());

            services.RemoveAll<IAIService>();
            services.AddSingleton<FakeAiService>();
            services.AddSingleton<IAIService>(sp => sp.GetRequiredService<FakeAiService>());

            services.RemoveAll<IEmailService>();
            services.RemoveAll<IBackgroundEmailService>();
            services.AddSingleton<FakeEmailSink>();
            services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<FakeEmailSink>());
            services.AddSingleton<IBackgroundEmailService>(sp => sp.GetRequiredService<FakeEmailSink>());
        });
    }

    // ---------------------------------------------------------------- helpers

    public HttpClient Anonymous() => CreateClient();

    public HttpClient As(int userId)
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", MintToken(userId));
        return c;
    }

    public HttpClient AsRole(TestRole role) => As(Ids.UserId(role));

    public string MintToken(int userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        var u = db.Users.Single(x => x.UserId == userId);
        int? sid = db.Students.Where(s => s.UserId == userId).Select(s => (int?)s.StudentId).FirstOrDefault();
        int? pid = db.Parents.Where(p => p.UserId == userId).Select(p => (int?)p.ParentId).FirstOrDefault();
        return jwt.GenerateToken(u, sid, pid);
    }

    public async Task<T> Db<T>(Func<AppDbContext, Task<T>> read)
    {
        using var scope = Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Xoá 1 entry trong <c>IMemoryCache</c> (SystemConfig cache 5′) sau khi sửa DB.</summary>
    public void BustCache(string cacheKey)
        => Services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>().Remove(cacheKey);

    public Task Db(Func<AppDbContext, Task> act) => Db<int>(async db => { await act(db); return 0; });
}
