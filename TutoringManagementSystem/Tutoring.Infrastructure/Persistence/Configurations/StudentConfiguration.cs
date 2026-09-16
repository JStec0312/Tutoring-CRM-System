using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration
    : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(student => student.Id);

        builder.Property(student => student.Id)
            .HasGeneratedStronglyTypedId(
                value => new StudentId(value));

        builder.Property(student => student.UserAccountId)
            .HasNullableStronglyTypedId(
                value => new UserAccountId(value),
                "UserAccountId")
            .IsRequired(false);

        builder.ComplexProperty(
            student => student.DisplayName,
            displayName =>
            {
                displayName.IsRequired();

                displayName.Property(valueObject => valueObject.Value)
                    .HasColumnName("DisplayName")
                    .HasColumnType("nvarchar(100)")
                    .HasMaxLength(100)
                    .IsRequired();
            });

        builder.Property(student => student.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(student => student.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(student => student.Account)
            .WithOne()
            .HasForeignKey<Student>(
                student => student.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(student => student.UserAccountId)
            .IsUnique()
            .HasFilter("[UserAccountId] IS NOT NULL");
    }
}