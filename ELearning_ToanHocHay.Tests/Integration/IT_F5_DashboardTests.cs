using System.Net;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F5 — Tiến độ &amp; Dashboard học sinh (IT-F5).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Dashboard")]
public class IT_F5_DashboardTests : IntegrationTest
{
    public IT_F5_DashboardTests(ApiFactory app) : base(app) { }

    private static readonly string[] TieredEndpoints =
        { "overview", "chapter-score-comparison", "ai-assessment", "ai-roadmap" };

    [SkippableFact] // IT-F5-01
    public async Task IT_F5_01_Overview_returns_data_for_the_owner()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();

        var res = await App.As(userId).GetAsync($"/api/students/{studentId}/dashboard/overview");
        await res.ShouldBeOk();
        (await res.DataAsync()).ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
    }

    [SkippableFact] // IT-F5-04
    public async Task IT_F5_04_Tier_comes_from_Package_Tier_not_the_name()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        // Gói tên "Standard" nhưng Tier = Premium — endpoint cần Premium phải mở.
        await Flow.GrantEntitlementAsync(studentId, EntitlementScope.AllContent,
            tier: PackageTier.Premium, packageName: "Standard");

        var res = await App.As(userId).GetAsync($"/api/students/{studentId}/dashboard/ai-assessment");
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F5-02
    public async Task IT_F5_02_Chapter_score_comparison_for_a_standard_student()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        await Flow.GrantEntitlementAsync(studentId, EntitlementScope.AllContent, tier: PackageTier.Standard);

        var res = await App.As(userId).GetAsync($"/api/students/{studentId}/dashboard/chapter-score-comparison");
        await res.ShouldBeOk();
    }

    [SkippableFact] // IT-F5-05
    public async Task IT_F5_05_Free_student_hitting_a_gated_endpoint_gets_403_not_500()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();

        var res = await App.As(userId).GetAsync($"/api/students/{studentId}/dashboard/chapter-score-comparison");

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.RootAsync()).GetProperty("Message").GetString().Should().Contain("gói");
    }

    [SkippableFact] // IT-F5-06
    public async Task IT_F5_06_Student_B_cannot_read_student_A_dashboards()
    {
        RequireDocker();
        var b = App.AsRole(TestRole.StudentB);

        foreach (var ep in TieredEndpoints)
            (await b.GetAsync($"/api/students/{Ids.StudentAId}/dashboard/{ep}"))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden, $"endpoint {ep}");
    }

    [SkippableFact] // IT-F5-07
    public async Task IT_F5_07_Dashboard_stats_requires_auth()
    {
        RequireDocker();
        (await App.Anonymous().GetAsync("/api/students/dashboard-stats")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await App.AsRole(TestRole.StudentA).GetAsync("/api/students/dashboard-stats")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact] // IT-F5-03
    public async Task IT_F5_03_Ai_assessment_and_roadmap_return_data_for_a_premium_student()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        await Flow.GrantEntitlementAsync(studentId, EntitlementScope.AllContent, tier: PackageTier.Premium);
        var client = App.As(userId);

        var assess = await client.GetAsync($"/api/students/{studentId}/dashboard/ai-assessment");
        await assess.ShouldBeOk();
        var data = await assess.DataAsync();
        // "không rỗng" — có tóm tắt hoặc danh sách khái niệm cần ôn (DTO dùng snake_case)
        var hasSummary = data.TryGetProperty("summary", out var s) && !string.IsNullOrWhiteSpace(s.GetString());
        var hasConcepts = data.TryGetProperty("concepts_to_review", out var c)
            && c.ValueKind == System.Text.Json.JsonValueKind.Array && c.GetArrayLength() > 0;
        (hasSummary || hasConcepts).Should().BeTrue();

        (await client.GetAsync($"/api/students/{studentId}/dashboard/ai-roadmap"))
            .StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F5-09
    public async Task IT_F5_09_Heatmap_returns_only_days_with_activity_within_the_window()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // chuỗi có ngày trống ở giữa
        await Flow.SeedDailyActivityAsync(studentId, today);
        await Flow.SeedDailyActivityAsync(studentId, today.AddDays(-1));
        await Flow.SeedDailyActivityAsync(studentId, today.AddDays(-3)); // -2 để trống
        await Flow.SeedDailyActivityAsync(studentId, today.AddDays(-120)); // ngoài cửa sổ 90 ngày

        var data = await (await App.As(userId).GetAsync($"/api/progress/students/{studentId}/heatmap?days=90")).DataAsync();
        var dates = data.EnumerateArray().Select(x => DateOnly.Parse(x.GetProperty("Date").GetString()!)).ToList();

        dates.Should().BeInAscendingOrder();
        dates.Should().HaveCount(3);
        dates.Should().OnlyContain(d => d >= today.AddDays(-89));
        dates.Should().NotContain(today.AddDays(-2));
    }

    [SkippableFact] // IT-F5-08
    public async Task IT_F5_08_Current_subscription_reports_active_package_or_free()
    {
        RequireDocker();
        var (freeUserId, freeStudentId) = await Flow.NewStudentAsync();
        var free = await App.As(freeUserId).GetAsync($"/api/students/{freeStudentId}/subscription/current");
        await free.ShouldBeOk();
        (await free.RootAsync()).GetProperty("Message").GetString().Should().Contain("Free");

        var (paidUserId, paidStudentId) = await Flow.NewStudentAsync();
        await Flow.GrantEntitlementAsync(paidStudentId, EntitlementScope.AllContent, packageName: "Gói Vàng");
        var paid = await App.As(paidUserId).GetAsync($"/api/students/{paidStudentId}/subscription/current");
        await paid.ShouldBeOk();
        (await paid.RootAsync()).GetProperty("Message").GetString().Should().Contain("Gói Vàng");
    }
}
