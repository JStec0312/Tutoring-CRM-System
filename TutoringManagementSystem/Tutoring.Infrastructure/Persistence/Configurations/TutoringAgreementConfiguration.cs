using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Students;
using Tutoring.Domain.Tutors;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class TutoringAgreementConfiguration
    : IEntityTypeConfiguration<TutoringAgreement>
{
    public void Configure(EntityTypeBuilder<TutoringAgreement> builder)
    {
        builder.ToTable("TutoringAgreements");

        builder.HasKey(agreement => agreement.Id);

        builder.Property(agreement => agreement.Id)
            .HasGeneratedStronglyTypedId(value => new TutoringAgreementId(value));

        builder.Property(agreement => agreement.TutorId)
            .HasStronglyTypedId(value => new TutorId(value), "TutorId")
            .IsRequired();

        builder.Property(agreement => agreement.StudentId)
            .HasStronglyTypedId(value => new StudentId(value), "StudentId")
            .IsRequired();

        builder.OwnsOne(agreement => agreement.Subject, subject =>
        {
            subject.Property(valueObject => valueObject.Name)
                .HasColumnName("SubjectName")
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(agreement => agreement.Subject)
            .IsRequired();

        builder.OwnsOne(agreement => agreement.HourlyRate, hourlyRate =>
        {
            hourlyRate.OwnsOne(valueObject => valueObject.PricePerHour, money =>
            {
                money.ConfigureMoney("HourlyRateAmount", "HourlyRateCurrencyCode");
            });

            hourlyRate.Navigation(valueObject => valueObject.PricePerHour)
                .IsRequired();
        });

        builder.Navigation(agreement => agreement.HourlyRate)
            .IsRequired();

        builder.Property(agreement => agreement.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(agreement => agreement.PrivateNotes)
            .HasColumnType("nvarchar(2000)")
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(agreement => agreement.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(agreement => agreement.Tutor)
            .WithMany()
            .HasForeignKey(agreement => agreement.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(agreement => agreement.Student)
            .WithMany()
            .HasForeignKey(agreement => agreement.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(agreement => agreement.TutorId);

        builder.HasIndex(agreement => agreement.StudentId);
    }
}
