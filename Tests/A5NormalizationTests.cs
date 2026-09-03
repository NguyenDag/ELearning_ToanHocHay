using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>A5 — consistent ApiResponse envelope, HTTP status semantics, kebab-case routes.</summary>
[Collection(IntegrationCollection.Name)]
public class A5NormalizationTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public A5NormalizationTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    private static void ShouldBeEnvelope(JsonElement root)
    {
        root.TryGetProperty("Success", out _).Should().BeTrue();
        root.TryGetProperty("Message", out _).Should().BeTrue();
    }

    [SkippableFact]
    public async Task Success_responses_use_the_ApiResponse_envelope()
    {
        RequireDocker();
        var root = await Root(await _f.CreateClient().GetAsync("/api/catalog/subjects"));
        ShouldBeEnvelope(root);
        root.GetProperty("Success").GetBoolean().Should().BeTrue();
        root.TryGetProperty("Data", out _).Should().BeTrue();
    }

    [SkippableFact]
    public async Task Missing_resource_is_404_with_an_envelope_and_consistent_across_lookups()
    {
        RequireDocker();
        var admin = _f.ClientFor(Id.AdminUserId);

        var byId = await admin.GetAsync("/api/users/999999");
        byId.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ShouldBeEnvelope(await Root(byId));

        // the review's concrete complaint: GetByEmail 404 vs GetById 400 — now both 404
        var byEmail = await admin.GetAsync("/api/users/email/nobody-here@test.local");
        byEmail.StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await _f.ClientFor(Id.ContentEditorUserId).GetAsync("/api/exercises/999999"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task Forbidden_is_403_with_an_envelope()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.StudentAUserId).PostAsJsonAsync("/api/catalog/subjects",
            new { Code = "X", Name = "X", Slug = "x" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var root = await Root(res);
        ShouldBeEnvelope(root);
        root.GetProperty("Success").GetBoolean().Should().BeFalse();
    }

    [SkippableFact]
    public async Task Model_validation_failure_is_400_with_errors_in_the_envelope()
    {
        RequireDocker();
        // reset-password requires a token + a >=6 char password
        var res = await _f.CreateClient().PostAsJsonAsync("/api/auth/reset-password",
            new { Token = "", NewPassword = "x" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await Root(res);
        ShouldBeEnvelope(root);
        root.GetProperty("Errors").GetArrayLength().Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task Old_PascalCase_routes_are_gone()
    {
        RequireDocker();
        var admin = _f.ClientFor(Id.AdminUserId);

        (await admin.GetAsync("/api/User")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _f.ClientFor(Id.StudentAUserId).PostAsJsonAsync("/api/exerciseattempts/start", new { ExerciseId = 1 }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound); // route no longer exists
    }
}
