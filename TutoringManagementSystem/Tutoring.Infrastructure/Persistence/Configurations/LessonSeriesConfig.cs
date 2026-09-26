using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.LessonSeries;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence.Configurations;
namespace Tutoring.Infrastructure.Persistence.Configurations;
internal sealed class LessonSeriesConfiguration
    : IEntityTypeConfiguration<LessonSeries>
{
    public void Configure(
        EntityTypeBuilder<LessonSeries> builder)
    {
        builder.ToTable("LessonSeries");

        builder.HasKey(series => series.Id);

        builder.Property(series => series.Id)
            .HasStronglyTypedEntityId(
                value => new LessonSeriesId(value));

        builder.Property(series => series.TutoringAgreementId)
            .HasStronglyTypedId(
                value => new TutoringAgreementId(value),
                "TutoringAgreementId")
            .IsRequired();

        builder.Property(series => series.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(series => series.StartsAt)
            .IsRequired();

        builder.Property(series => series.Duration)
            .IsRequired();

        builder.Property(series => series.StartsOn)
            .IsRequired();

        builder.Property(series => series.EndsOn)
            .IsRequired(false);

        builder.Property(series => series.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne<TutoringAgreement>()
            .WithMany()
            .HasForeignKey(series => series.TutoringAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(series =>
            series.TutoringAgreementId);
    }
}