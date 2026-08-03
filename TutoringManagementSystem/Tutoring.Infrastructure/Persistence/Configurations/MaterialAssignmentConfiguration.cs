using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class MaterialAssignmentConfiguration
    : IEntityTypeConfiguration<MaterialAssignment>
{
    public void Configure(EntityTypeBuilder<MaterialAssignment> builder)
    {
        builder.ToTable("MaterialAssignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasGeneratedStronglyTypedId(value => new MaterialAssignmentId(value));

        builder.Property(assignment => assignment.LearningMaterialId)
            .HasStronglyTypedId(value => new LearningMaterialId(value), "LearningMaterialId")
            .IsRequired();

        builder.Property(assignment => assignment.TutoringAgreementId)
            .HasStronglyTypedId(value => new TutoringAgreementId(value), "TutoringAgreementId")
            .IsRequired();

        var lessonIdConverter = new ValueConverter<LessonId?, Guid?>(
            lessonId => lessonId == null ? null : lessonId.Value.Value,
            value => value == null ? null : new LessonId(value.Value));

        builder.Property(assignment => assignment.LessonId)
            .HasConversion(lessonIdConverter)
            .HasColumnName("LessonId")
            .HasColumnType("uniqueidentifier")
            .IsRequired(false);

        builder.Property(assignment => assignment.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(assignment => assignment.AssignedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(assignment => assignment.LearningMaterial)
            .WithMany()
            .HasForeignKey(assignment => assignment.LearningMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Agreement)
            .WithMany()
            .HasForeignKey(assignment => assignment.TutoringAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Lesson)
            .WithMany()
            .HasForeignKey(assignment => assignment.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => assignment.LearningMaterialId);

        builder.HasIndex(assignment => assignment.TutoringAgreementId);

        builder.HasIndex(assignment => assignment.LessonId);

        builder.HasIndex(assignment => new
            {
                assignment.LearningMaterialId,
                assignment.TutoringAgreementId,
                assignment.LessonId
            })
            .IsUnique()
            .HasFilter("[LessonId] IS NOT NULL");

        builder.HasIndex(assignment => new
            {
                assignment.LearningMaterialId,
                assignment.TutoringAgreementId
            })
            .IsUnique()
            .HasFilter("[LessonId] IS NULL");
    }
}
