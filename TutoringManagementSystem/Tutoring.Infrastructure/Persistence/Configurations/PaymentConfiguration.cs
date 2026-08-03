using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tutoring.Domain.Billing;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration
    : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id)
            .HasGeneratedStronglyTypedId(value => new PaymentId(value));

        builder.Property(payment => payment.BillingAccountId)
            .HasStronglyTypedId(value => new BillingAccountId(value), "BillingAccountId")
            .IsRequired();

        builder.OwnsOne(payment => payment.Amount, amount =>
        {
            amount.ConfigureMoney("Amount", "CurrencyCode");
        });

        builder.Navigation(payment => payment.Amount)
            .IsRequired();

        builder.Property(payment => payment.PaidAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        var paymentReferenceConverter = new ValueConverter<PaymentReference?, string?>(
            reference => reference == null ? null : reference.Value,
            value => value == null ? null : new PaymentReference(value));

        builder.Property(payment => payment.Reference)
            .HasConversion(paymentReferenceConverter)
            .HasColumnName("Reference")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.HasOne<BillingAccount>()
            .WithMany(account => account.Payments)
            .HasForeignKey(payment => payment.BillingAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(payment => payment.BillingAccountId);
    }
}
