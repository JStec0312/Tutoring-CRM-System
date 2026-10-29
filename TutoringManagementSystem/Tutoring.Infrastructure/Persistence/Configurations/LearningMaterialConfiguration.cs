using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class LearningMaterialConfiguration
    : IEntityTypeConfiguration<LearningMaterial>
{
    public void Configure(EntityTypeBuilder<LearningMaterial> builder)
    {
        builder.ToTable("LearningMaterials");

        builder.HasKey(material => material.Id);

        builder.Property(material => material.Id)
            .HasStronglyTypedEntityId(value => new LearningMaterialId(value));

        builder.Property(material => material.TutoringAgreementId)
            .HasStronglyTypedId(value => new TutoringAgreementId(value), "TutoringAgreementId")
            .IsRequired();

        builder.Property(material => material.UploadedByUserAccountId)
            .HasStronglyTypedId(value => new UserAccountId(value), "UploadedByUserAccountId")
            .IsRequired();
        builder.Property(material => material.LessonId)
            .HasConversion(new ValueConverter<LessonId?, Guid?>(
                id => id == null ? null : id.Value.Value,
                value => value == null ? null : new LessonId(value.Value)))
            .HasColumnType("uniqueidentifier");
        builder.HasOne<TutoringAgreement>().WithMany()
            .HasForeignKey(material => material.TutoringAgreementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lesson>().WithMany()
            .HasForeignKey(material => material.LessonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany()
            .HasForeignKey(material => material.UploadedByUserAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(material => material.Title)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsOne(material => material.File, file =>
        {
            file.Property(valueObject => valueObject.FileName)
                .HasColumnName("FileName")
                .HasColumnType("nvarchar(260)")
                .HasMaxLength(260)
                .IsRequired();

            file.Property(valueObject => valueObject.ContentType)
                .HasColumnName("ContentType")
                .HasColumnType("nvarchar(255)")
                .HasMaxLength(255)
                .IsRequired();

            file.OwnsOne(valueObject => valueObject.Size, size =>
            {
                size.Property(valueObject => valueObject.Bytes)
                    .HasColumnName("FileSizeBytes")
                    .HasColumnType("bigint")
                    .IsRequired();
            });

            file.Navigation(valueObject => valueObject.Size)
                .IsRequired();

            file.OwnsOne(valueObject => valueObject.StorageLocation, storageLocation =>
            {
                storageLocation.Property(valueObject => valueObject.Value)
                    .HasColumnName("StorageLocation")
                    .HasColumnType("nvarchar(1024)")
                    .HasMaxLength(1024)
                    .IsRequired();
            });

            file.Navigation(valueObject => valueObject.StorageLocation)
                .IsRequired();
        });

        builder.Navigation(material => material.File)
            .IsRequired();

        builder.Property(material => material.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(material => material.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasIndex(material => material.TutoringAgreementId);
    }
}
