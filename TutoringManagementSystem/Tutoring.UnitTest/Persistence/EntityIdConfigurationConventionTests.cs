using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tutoring.Domain.Common;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.UnitTest.Persistence;

public sealed class EntityIdConfigurationConventionTests
{
    public static IEnumerable<object[]> PersistedDomainEntityTypes()
    {
        using var context = CreateContext();

        return context.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(IsDomainEntityType)
            .Select(clrType => new object[] { clrType })
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(PersistedDomainEntityTypes))]
    public void Persisted_domain_entity_primary_key_should_not_be_database_generated(Type entityClrType)
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(entityClrType);

        Assert.NotNull(entityType);

        var idProperty = entityType!.FindProperty("Id");

        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty!.ValueGenerated);
    }

    private static TutoringDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TutoringDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=TutoringConventionTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new TutoringDbContext(options);
    }

    private static bool IsDomainEntityType(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return true;
            }
        }

        return false;
    }
}
