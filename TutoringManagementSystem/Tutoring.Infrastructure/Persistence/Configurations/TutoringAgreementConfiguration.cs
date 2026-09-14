using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Students;
using Tutoring.Domain.Tutors;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class TutoringAgreementConfiguration
    : IEntityTypeConfiguration<TutoringAgreement>
{
    public void Configure(
        EntityTypeBuilder<TutoringAgreement> builder)
    {
        builder.ToTable("TutoringAgreements");

        builder.HasKey(agreement => agreement.Id);

        builder.Property(agreement => agreement.Id)
            .HasGeneratedStronglyTypedId(
                value => new TutoringAgreementId(value));

        builder.Property(agreement => agreement.TutorId)
            .HasStronglyTypedId(
                value => new TutorId(value),
                "TutorId")
            .IsRequired();

        builder.Property(agreement => agreement.StudentId)
            .HasStronglyTypedId(
                value => new StudentId(value),
                "StudentId")
            .IsRequired();

        builder.ComplexProperty(
            agreement => agreement.AgreementTitle,
            title =>
            {
                title.IsRequired();

                title.Property(valueObject => valueObject.Value)
                    .HasColumnName("AgreementTitle")
                    .HasColumnType("nvarchar(100)")
                    .HasMaxLength(100)
                    .IsRequired();
            });

        builder.ComplexProperty(
            agreement => agreement.Subject,
            subject =>
            {
                subject.IsRequired();

                subject.Property(valueObject => valueObject.Name)
                    .HasColumnName("SubjectName")
                    .HasColumnType("nvarchar(100)")
                    .HasMaxLength(100)
                    .IsRequired();
            });

        builder.ComplexProperty(
            agreement => agreement.HourlyRate,
            hourlyRate =>
            {
                hourlyRate.IsRequired(false);
                hourlyRate.HasDiscriminator();

                hourlyRate.ComplexProperty(
                    valueObject => valueObject.PricePerHour,
                    money =>
                    {
                        money.IsRequired();

                        money.Property(valueObject => valueObject.Amount)
                            .HasColumnName("HourlyRateAmount")
                            .HasColumnType("decimal(18,2)")
                            .HasPrecision(18, 2)
                            .IsRequired();

                        money.Property(valueObject => valueObject.Currency)
                            .HasConversion(
                                currency => currency.Code,
                                code => new Currency(code))
                            .HasColumnName("HourlyRateCurrencyCode")
                            .HasColumnType("char(3)")
                            .HasMaxLength(3)
                            .IsRequired();
                    });
            });

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