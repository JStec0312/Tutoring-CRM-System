using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Lessons;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class LessonNoteConfiguration
    : IEntityTypeConfiguration<LessonNote>
{
    public void Configure(EntityTypeBuilder<LessonNote> builder)
    {
        builder.ToTable("LessonNotes");

        builder.HasKey(note => note.Id);

        builder.Property(note => note.Id)
            .HasGeneratedStronglyTypedId(value => new LessonNoteId(value));

        builder.Property(note => note.LessonId)
            .HasStronglyTypedId(value => new LessonId(value), "LessonId")
            .IsRequired();

        builder.Property(note => note.Topic)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(note => note.CoveredTopics)
            .HasColumnType("nvarchar(2000)")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(note => note.StudentProgress)
            .HasColumnType("nvarchar(2000)")
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasOne<Lesson>()
            .WithOne(lesson => lesson.Note)
            .HasForeignKey<LessonNote>(note => note.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(note => note.LessonId)
            .IsUnique();
    }
}
