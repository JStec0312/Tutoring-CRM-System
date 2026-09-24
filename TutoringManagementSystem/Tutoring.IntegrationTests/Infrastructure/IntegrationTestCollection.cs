namespace Tutoring.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "IntegrationTests";
}