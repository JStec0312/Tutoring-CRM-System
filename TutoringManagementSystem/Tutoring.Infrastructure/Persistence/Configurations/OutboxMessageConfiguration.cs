using Microsoft.EntityFrameworkCore;

namespace Tutoring.Infrastructure.Persistence.Auth;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Infrastructure.Mailing;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "messaging");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .HasColumnType("datetimeoffset");

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(4000);

        builder.HasIndex(x => new
        {
            x.ProcessedAtUtc,
            x.OccurredAtUtc
        });
    }
}