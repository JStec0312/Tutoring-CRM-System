using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Infrastructure.Email;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class EmailOutboxMessageConfiguration
    : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable("EmailOutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedNever();

        builder.Property(message => message.Recipient)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(message => message.Subject)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.HtmlBody)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(message => message.CreatedAtUtc)
            .IsRequired();

        builder.Property(message => message.SentAtUtc)
            .IsRequired(false);

        builder.Property(message => message.Attempts)
            .IsRequired();

        builder.Property(message => message.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.HasIndex(message => message.SentAtUtc);
    }
}