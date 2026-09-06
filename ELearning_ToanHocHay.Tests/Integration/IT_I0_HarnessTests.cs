using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>I0 — xác nhận hạ tầng integration tự boot được (hoặc skip sạch khi không có Docker).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Harness")]
public class IT_I0_HarnessTests : IntegrationTest
{
    public IT_I0_HarnessTests(ApiFactory app) : base(app) { }

    [SkippableFact]
    public void Golden_dataset_is_seeded()
    {
        RequireDocker();
        Ids.Should().NotBeNull();
        Ids.StudentAUserId.Should().BeGreaterThan(0);
        Ids.QuizExerciseId.Should().BeGreaterThan(0);
        Ids.UserId(TestRole.Admin).Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task Health_endpoint_is_up()
    {
        RequireDocker();
        var res = await App.Anonymous().GetAsync("/health");
        res.IsSuccessStatusCode.Should().BeTrue();
    }

    [SkippableFact]
    public async Task Authenticated_client_can_reach_me()
    {
        RequireDocker();
        var res = await App.AsRole(TestRole.StudentA).GetAsync("/api/auth/me");
        await res.ShouldBeOk();
        var data = await res.DataAsync();
        data.GetProperty("UserType").GetString().Should().Be("Student");
    }
}
