using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class LessonChargeConfiguration
    : IEntityTypeConfiguration<LessonCharge>
{
    public void Configure(EntityTypeBuilder<LessonCharge> builder)
    {
        builder.ToTable("LessonCharges");

        builder.HasKey(charge => charge.Id);

        builder.Property(charge => charge.Id)
            .HasGeneratedStronglyTypedId(
                value => new LessonChargeId(value));

        builder.Property(charge => charge.BillingAccountId)
            .HasStronglyTypedId(
                value => new BillingAccountId(value),
                "BillingAccountId")
            .IsRequired();

        builder.Property(charge => charge.LessonId)
            .HasStronglyTypedId(
                value => new LessonId(value),
                "LessonId")
            .IsRequired();

        builder.OwnsOne(charge => charge.Amount, amount =>
        {
            amount.ConfigureMoney(
                "Amount",
                "CurrencyCode");
        });

        builder.Navigation(charge => charge.Amount)
            .IsRequired();

        builder.Property(charge => charge.ChargedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(charge => charge.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(charge => charge.Description)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(charge => charge.Lesson)
            .WithMany()
            .HasForeignKey(charge => charge.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(charge => charge.BillingAccountId);

        builder.HasIndex(charge => charge.LessonId)
            .IsUnique();
    }
}