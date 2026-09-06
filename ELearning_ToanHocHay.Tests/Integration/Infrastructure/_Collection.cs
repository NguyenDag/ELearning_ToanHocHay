using Xunit;

// Cho phép song song GIỮA các collection; integration nằm chung 1 collection nên vẫn tuần tự.
[assembly: CollectionBehavior(DisableTestParallelization = false)]

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "integration";
}

/// <summary>Lớp nền cho mọi class integration test.</summary>
[Trait("Level", "Integration")]
public abstract class IntegrationTest
{
    protected readonly ApiFactory App;
    protected SeededIds Ids => App.Ids;
    protected FlowSeed Flow => new(App);

    protected IntegrationTest(ApiFactory app) => App = app;

    /// <summary>Dòng đầu mỗi test: bỏ qua khi máy không có Docker.</summary>
    protected void RequireDocker()
        => Skip.IfNot(App.DockerAvailable, "Docker không khả dụng — bỏ qua integration test.");
}
