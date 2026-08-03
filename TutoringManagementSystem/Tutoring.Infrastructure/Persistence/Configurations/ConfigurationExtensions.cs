using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Common;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static PropertyBuilder<TId> HasStronglyTypedId<TId>(
        this PropertyBuilder<TId> builder,
        Func<Guid, TId> factory,
        string columnName)
        where TId : struct, IDomainId
    {
        return builder
            .HasConversion(
                id => id.Value,
                value => factory(value))
            .HasColumnName(columnName)
            .HasColumnType("uniqueidentifier");
    }

    public static PropertyBuilder<TId> HasGeneratedStronglyTypedId<TId>(
        this PropertyBuilder<TId> builder,
        Func<Guid, TId> factory)
        where TId : struct, IDomainId
    {
        return builder
            .HasStronglyTypedId(factory, "Id")
            .HasColumnType("uniqueidentifier")
            .HasDefaultValueSql("newsequentialid()")
            .ValueGeneratedOnAdd();
    }

    public static void ConfigureMoney<TOwner>(
        this OwnedNavigationBuilder<TOwner, Money> builder,
        string amountColumnName,
        string currencyColumnName)
        where TOwner : class
    {
        builder.Property(money => money.Amount)
            .HasColumnName(amountColumnName)
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(money => money.Currency)
            .HasConversion(
                currency => currency.Code,
                code => new Currency(code))
            .HasColumnName(currencyColumnName)
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();
    }
}
