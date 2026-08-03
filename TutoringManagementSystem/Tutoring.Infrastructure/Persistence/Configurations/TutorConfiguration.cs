using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Tutors;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class TutorConfiguration
    : IEntityTypeConfiguration<Tutor>
{
    public void Configure(EntityTypeBuilder<Tutor> builder)
    {
        builder.ToTable("Tutors");

        builder.HasKey(tutor => tutor.Id);

        builder.Property(tutor => tutor.Id)
            .HasGeneratedStronglyTypedId(value => new TutorId(value));

        builder.Property(tutor => tutor.UserAccountId)
            .HasStronglyTypedId(value => new UserAccountId(value), "UserAccountId")
            .IsRequired();

        builder.Property(tutor => tutor.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(tutor => tutor.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(tutor => tutor.Account)
            .WithOne()
            .HasForeignKey<Tutor>(tutor => tutor.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tutor => tutor.UserAccountId)
            .IsUnique();
    }
}
