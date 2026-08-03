using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Billing;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class BillingAccountConfiguration
    : IEntityTypeConfiguration<BillingAccount>
{
    public void Configure(EntityTypeBuilder<BillingAccount> builder)
    {
        builder.ToTable("BillingAccounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasGeneratedStronglyTypedId(value => new BillingAccountId(value));

        builder.Property(account => account.TutoringAgreementId)
            .HasStronglyTypedId(value => new TutoringAgreementId(value), "TutoringAgreementId")
            .IsRequired();

        builder.Property(account => account.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(account => account.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(account => account.Agreement)
            .WithOne()
            .HasForeignKey<BillingAccount>(account => account.TutoringAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(account => account.Charges)
            .WithOne()
            .HasForeignKey(charge => charge.BillingAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(account => account.Payments)
            .WithOne()
            .HasForeignKey(payment => payment.BillingAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(account => account.Charges)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(account => account.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(account => account.TutoringAgreementId)
            .IsUnique();
    }
}
