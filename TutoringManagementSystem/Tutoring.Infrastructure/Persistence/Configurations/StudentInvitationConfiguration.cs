using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Domain.Tutors;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class StudentInvitationConfiguration
    : IEntityTypeConfiguration<StudentInvitation>
{
    public void Configure(EntityTypeBuilder<StudentInvitation> builder)
    {
        builder.ToTable("StudentInvitations");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Id)
            .HasGeneratedStronglyTypedId(value => new StudentInvitationId(value));

        builder.Property(invitation => invitation.TutorId)
            .HasStronglyTypedId(value => new TutorId(value), "TutorId")
            .IsRequired();

        builder.OwnsOne(invitation => invitation.Recipient, recipient =>
        {
            recipient.Property(valueObject => valueObject.Value)
                .HasColumnName("RecipientEmail")
                .HasColumnType("nvarchar(320)")
                .HasMaxLength(320)
                .IsRequired();
        });

        builder.Navigation(invitation => invitation.Recipient)
            .IsRequired();

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
