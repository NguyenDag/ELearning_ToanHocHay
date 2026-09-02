namespace ELearning_ToanHocHay_Control.Tests.Infrastructure;

/// <summary>
/// Shares a single <see cref="A1TestFactory"/> (one container, one seed) across all
/// integration test classes so they run sequentially — the env-var based DB wiring
/// is not safe to run in parallel.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<A1TestFactory>
{
    public const string Name = "integration";
}
