using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class LessonConfiguration
    : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");

        builder.HasKey(lesson => lesson.Id);

        builder.Property(lesson => lesson.Id)
            .HasGeneratedStronglyTypedId(value => new LessonId(value));

        builder.Property(lesson => lesson.TutoringAgreementId)
            .HasStronglyTypedId(value => new TutoringAgreementId(value), "TutoringAgreementId")
            .IsRequired();

        builder.OwnsOne(lesson => lesson.TimeSlot, timeSlot =>
        {
            timeSlot.Property(valueObject => valueObject.StartsAtUtc)
                .HasColumnName("StartsAtUtc")
                .HasColumnType("datetimeoffset")
                .IsRequired();

            timeSlot.Property(valueObject => valueObject.EndsAtUtc)
                .HasColumnName("EndsAtUtc")
                .HasColumnType("datetimeoffset")
                .IsRequired();
        });

        builder.Navigation(lesson => lesson.TimeSlot)
            .IsRequired();

        builder.Property(lesson => lesson.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        var cancellationReasonConverter = new ValueConverter<CancellationReason?, string?>(
            cancellationReason => cancellationReason == null ? null : cancellationReason.Text,
            value => value == null ? null : new CancellationReason(value));

        builder.Property(lesson => lesson.CancellationReason)
            .HasConversion(cancellationReasonConverter)
            .HasColumnName("CancellationReason")
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(lesson => lesson.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(lesson => lesson.Agreement)
            .WithMany()
            .HasForeignKey(lesson => lesson.TutoringAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lesson => lesson.Note)
            .WithOne()
            .HasForeignKey<LessonNote>(note => note.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lesson => lesson.TutoringAgreementId);
    }
}
