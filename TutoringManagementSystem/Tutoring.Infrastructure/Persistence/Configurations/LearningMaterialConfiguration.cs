using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Tutors;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class LearningMaterialConfiguration
    : IEntityTypeConfiguration<LearningMaterial>
{
    public void Configure(EntityTypeBuilder<LearningMaterial> builder)
    {
        builder.ToTable("LearningMaterials");

        builder.HasKey(material => material.Id);

        builder.Property(material => material.Id)
            .HasGeneratedStronglyTypedId(value => new LearningMaterialId(value));

        builder.Property(material => material.TutorId)
            .HasStronglyTypedId(value => new TutorId(value), "TutorId")
            .IsRequired();

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

        builder.HasOne(material => material.Tutor)
            .WithMany()
            .HasForeignKey(material => material.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(material => material.TutorId);
    }
}
