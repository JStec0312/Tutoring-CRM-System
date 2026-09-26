using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Billing;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Domain.Tutors;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class StudentInvitationConfiguration
    : IEntityTypeConfiguration<StudentInvitation>
{
    public void Configure(
        EntityTypeBuilder<StudentInvitation> builder)
    {
        builder.ToTable("StudentInvitations");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Id)
            .HasStronglyTypedEntityId(
                value => new StudentInvitationId(value));

        builder.Property(invitation => invitation.TutorId)
            .HasStronglyTypedId(
                value => new TutorId(value),
                "TutorId")
            .IsRequired();

        builder.OwnsOne(
            invitation => invitation.Recipient,
            recipient =>
            {
                recipient.Property(valueObject => valueObject.Value)
                    .HasColumnName("RecipientEmail")
                    .HasColumnType("nvarchar(320)")
                    .HasMaxLength(320)
                    .IsRequired();
            });

        builder.Navigation(invitation => invitation.Recipient)
            .IsRequired();

        builder.OwnsOne(
            invitation => invitation.Title,
            title =>
            {
                title.Property(valueObject => valueObject.Value)
                    .HasColumnName("AgreementTitle")
                    .HasColumnType("nvarchar(100)")
                    .HasMaxLength(100)
                    .IsRequired();
            });

        builder.Navigation(invitation => invitation.Title)
            .IsRequired();

        builder.OwnsOne(
            invitation => invitation.Subject,
            subject =>
            {
                subject.Property(valueObject => valueObject.Name)
                    .HasColumnName("SubjectName")
                    .HasColumnType("nvarchar(100)")
                    .HasMaxLength(100)
                    .IsRequired();
            });

        builder.Navigation(invitation => invitation.Subject)
            .IsRequired();

        builder.ComplexProperty(
            invitation => invitation.HourlyRate,
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

        builder.Property(invitation => invitation.TokenHash)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(invitation => invitation.TokenHash)
            .IsUnique();

        builder.Property(invitation => invitation.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(invitation => invitation.ValidUntilUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(invitation => invitation.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(invitation => invitation.Tutor)
            .WithMany()
            .HasForeignKey(invitation => invitation.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invitation => invitation.TutorId);
    }
}