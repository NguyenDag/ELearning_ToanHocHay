using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// Contract / snapshot tests — pin the wire shape of the ApiResponse envelope and enum
/// serialisation so an accidental change is caught before the WebApp breaks — plus a light
/// concurrency smoke test. A real load test (200+ virtual users) belongs in a dedicated
/// k6 / JMeter run against staging; see docs/Ra-soat-API-va-ke-hoach-kiem-soat.md (A5).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ContractTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public ContractTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    // ---------- envelope ----------

    [SkippableFact]
    public async Task Success_envelope_has_the_documented_fields()
    {
        RequireDocker();
        var root = await Root(await _f.CreateClient().GetAsync("/api/catalog/subjects"));

        root.GetProperty("Success").ValueKind.Should().Be(JsonValueKind.True);
        root.GetProperty("Message").ValueKind.Should().Be(JsonValueKind.String);
        root.TryGetProperty("Data", out var data).Should().BeTrue();
        data.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetProperty("Errors").ValueKind.Should().Be(JsonValueKind.Array);
        // StatusCode is transport-only and must not leak into the body.
        root.TryGetProperty("StatusCode", out _).Should().BeFalse();
    }

    [SkippableFact]
    public async Task Error_envelope_has_the_documented_fields()
    {
        RequireDocker();
        var res = await _f.ClientFor(Id.AdminUserId).GetAsync("/api/users/999999");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await Root(res);
        root.GetProperty("Success").ValueKind.Should().Be(JsonValueKind.False);
        root.GetProperty("Message").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("Message").GetString().Should().NotBeNullOrWhiteSpace();
        root.TryGetProperty("Errors", out var errors).Should().BeTrue();
        errors.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [SkippableFact]
    public async Task Paged_result_shape_is_stable()
    {
        RequireDocker();
        var data = (await Root(await _f.ClientFor(Id.AdminUserId).GetAsync("/api/users?page=1&pageSize=2")))
            .GetProperty("Data");

        data.GetProperty("Items").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("Items").GetArrayLength().Should().Be(2);
        data.GetProperty("Page").GetInt32().Should().Be(1);
        data.GetProperty("PageSize").GetInt32().Should().Be(2);
        data.GetProperty("Total").GetInt32().Should().BeGreaterThan(2);
    }

    // ---------- enums serialise as strings (A5, JsonStringEnumConverter) ----------

    [SkippableFact]
    public async Task Enum_fields_serialise_as_their_string_name()
    {
        RequireDocker();

        var user = (await Root(await _f.ClientFor(Id.AdminUserId).GetAsync($"/api/users/{Id.StudentAUserId}")))
            .GetProperty("Data");
        var userType = user.GetProperty("UserType");
        userType.ValueKind.Should().Be(JsonValueKind.String, "enums must not serialise as numbers");
        userType.GetString().Should().Be("Student");
    }

    [SkippableFact]
    public async Task Enum_query_parameters_still_bind_from_a_string_name()
    {
        RequireDocker();
        // JsonStringEnumConverter must not break request binding — status=Pending by name.
        var res = await _f.ClientFor(Id.FinanceUserId)
            .GetAsync("/api/subscriptions?page=1&pageSize=5&status=Pending");
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
    }

    // ---------- concurrency smoke ----------

    [SkippableFact]
    public async Task Handles_a_burst_of_concurrent_reads()
    {
        RequireDocker();
        var client = _f.CreateClient();

        var sw = Stopwatch.StartNew();
        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => client.GetAsync("/api/catalog/subjects")));
        sw.Stop();

        results.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
            "100 cached catalog reads should not take anywhere near this long");
    }

    [SkippableFact]
    public async Task Concurrent_exercise_starts_by_one_student_never_return_5xx()
    {
        RequireDocker();
        var client = _f.ClientFor(Id.StudentBUserId);

        var results = await Task.WhenAll(Enumerable.Range(0, 15).Select(_ =>
            client.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = Id.ExerciseId })));

        foreach (var r in results)
            ((int)r.StatusCode).Should().BeLessThan(500,
                $"a concurrent start must fail cleanly, not with {r.StatusCode}");
        results.Should().Contain(r => r.StatusCode == HttpStatusCode.OK);
    }
}
