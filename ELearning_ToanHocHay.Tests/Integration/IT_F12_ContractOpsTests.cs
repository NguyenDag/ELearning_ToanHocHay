using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F12 — Hợp đồng API &amp; Vận hành (IT-F12).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Contract")]
public class IT_F12_ContractOpsTests : IntegrationTest
{
    public IT_F12_ContractOpsTests(ApiFactory app) : base(app) { }

    // ---------------------------------------------------------------- envelope

    [SkippableFact] // IT-F12-01
    public async Task IT_F12_01_Success_responses_use_the_envelope()
    {
        RequireDocker();
        var res = await App.Anonymous().GetAsync("/api/catalog/subjects");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = await res.RootAsync();
        root.GetProperty("Success").GetBoolean().Should().BeTrue();
        root.TryGetProperty("Message", out _).Should().BeTrue();
        root.TryGetProperty("Data", out var data).Should().BeTrue();
        data.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [SkippableFact] // IT-F12-02
    public async Task IT_F12_02_Missing_resources_are_404_with_a_consistent_envelope()
    {
        RequireDocker();
        foreach (var url in new[]
        {
            "/api/courses/99999999",
            "/api/courses/by-slug/khong-co-that-" + Guid.NewGuid().ToString("N"),
            "/api/catalog/subjects/99999999",
            "/api/packages/99999999",
        })
        {
            var res = await App.Anonymous().GetAsync(url);
            res.StatusCode.Should().Be(HttpStatusCode.NotFound, url);
            (await res.RootAsync()).GetProperty("Message").GetString().Should().NotBeNullOrWhiteSpace(url);
        }
    }

    [SkippableFact] // IT-F12-03
    public async Task IT_F12_03_Forbidden_is_403_with_a_non_empty_envelope()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.StudentA).GetAsync("/api/admin/audit-logs");

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.RootAsync()).GetProperty("Message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact] // IT-F12-04
    public async Task IT_F12_04_Model_validation_failures_are_400_with_errors()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.Editor).PostAsJsonAsync("/api/courses", new { });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await res.RootAsync();
        root.GetProperty("Errors").EnumerateArray().Should().NotBeEmpty();
    }

    // ---------------------------------------------------------------- enums / paging

    [SkippableFact] // IT-F12-05
    public async Task IT_F12_05_Enum_fields_serialise_as_their_string_name()
    {
        RequireDocker();
        var data = await (await App.AsRole(TestRole.StudentA).GetAsync("/api/auth/me")).DataAsync();
        data.GetProperty("UserType").ValueKind.Should().Be(JsonValueKind.String);
        data.GetProperty("UserType").GetString().Should().Be("Student");
    }

    [SkippableFact] // IT-F12-06
    public async Task IT_F12_06_Enum_query_parameters_bind_from_a_string()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.Finance).GetAsync("/api/subscriptions?status=Active&page=1&pageSize=5");
        await res.ShouldBeOk();
    }

    [SkippableFact] // IT-F12-07
    public async Task IT_F12_07_Paged_result_shape_is_stable()
    {
        RequireDocker();
        var (total, page, pageSize, items) = await
            (await App.AsRole(TestRole.Finance).GetAsync("/api/subscriptions?page=1&pageSize=5")).ShouldBePaged();

        page.Should().Be(1);
        pageSize.Should().Be(5);
        total.Should().BeGreaterThanOrEqualTo(0);
        items.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [SkippableFact] // IT-F12-08
    public async Task IT_F12_08_Old_pascalcase_routes_are_gone()
    {
        RequireDocker();
        foreach (var url in new[] { "/api/Subscription", "/api/User", "/api/Course", "/api/Payment" })
            (await App.AsRole(TestRole.Admin).GetAsync(url)).StatusCode
                .Should().Be(HttpStatusCode.NotFound, url);
    }

    // ---------------------------------------------------------------- ops

    [SkippableFact] // IT-F12-09
    public async Task IT_F12_09_Every_response_carries_a_correlation_id_and_echoes_the_inbound_one()
    {
        RequireDocker();

        var generated = await App.Anonymous().GetAsync("/api/catalog/subjects");
        generated.Headers.GetValues("X-Correlation-ID").Single().Should().NotBeNullOrWhiteSpace();

        var mine = "corr-" + Guid.NewGuid().ToString("N");
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/catalog/subjects");
        req.Headers.Add("X-Correlation-ID", mine);
        var echoed = await App.Anonymous().SendAsync(req);
        echoed.Headers.GetValues("X-Correlation-ID").Single().Should().Be(mine);
    }

    [SkippableFact] // IT-F12-10
    public async Task IT_F12_10_Health_endpoints_report_live_and_ready()
    {
        RequireDocker();
        (await App.Anonymous().GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await App.Anonymous().GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact] // IT-F12-11
    public async Task IT_F12_11_Sensitive_field_change_is_written_to_the_audit_log()
    {
        RequireDocker();
        var (userId, _, _) = await Flow.NewConfirmedUserAsync(UserType.SupportStaff);

        await (await App.AsRole(TestRole.Admin).PostAsJsonAsync($"/api/admin/users/{userId}/role", new { NewRole = "ContentEditor" }))
            .ShouldBeOk();

        await App.Db(async db =>
            (await db.AuditLogs.AnyAsync(a => a.EntityType == "User" && a.EntityId == userId)).Should().BeTrue());
    }

    [SkippableFact] // IT-F12-12
    public async Task IT_F12_12_Uncaught_exception_is_a_500_envelope_without_leaking_details()
    {
        RequireDocker();
        var res = await App.Anonymous().GetAsync("/api/_diag/boom");

        res.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
        body.Should().NotContain("secret-internal-detail");
        body.Should().NotContain("InvalidOperationException");
        body.Should().NotContain("at ELearning_ToanHocHay", "không lộ stack trace");
    }

    [SkippableFact] // IT-F12-13
    public async Task IT_F12_13_Cors_preflight_from_a_foreign_origin_is_not_allowed()
    {
        RequireDocker();
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/catalog/subjects");
        req.Headers.Add("Origin", "https://evil.example");
        req.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await App.Anonymous().SendAsync(req);
        res.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [SkippableFact] // IT-F12-14
    public async Task IT_F12_14_Rate_limited_endpoint_returns_a_429_envelope()
    {
        RequireDocker();
        using var throttled = App.WithWebHostBuilder(b =>
            b.UseSetting("RateLimiting:AuthPermitLimit", "2"));
        var client = throttled.CreateClient();

        async Task<HttpResponseMessage> Login() => await client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"ghost.{Guid.NewGuid():N}@flow.test", Password = "whatever" });

        await Login();
        await Login();
        var blocked = await Login();

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await blocked.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace("429 phải trả vỏ ApiResponse, không phải body rỗng");
        body.Should().Contain("quá nhanh");
    }

    [SkippableFact] // IT-F12-15
    public async Task IT_F12_15_Anonymous_hitting_a_protected_route_is_401_not_302()
    {
        RequireDocker();
        (await App.Anonymous().GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F12-16
    public async Task IT_F12_16_User_list_is_paged_searchable_and_pagesize_is_clamped()
    {
        RequireDocker();
        var (_, page, pageSize, _) = await
            (await App.AsRole(TestRole.Admin).GetAsync("/api/users?page=1&pageSize=999&search=it.test")).ShouldBePaged();

        page.Should().Be(1);
        pageSize.Should().Be(100, "pageSize được kẹp về 100");
    }
}
