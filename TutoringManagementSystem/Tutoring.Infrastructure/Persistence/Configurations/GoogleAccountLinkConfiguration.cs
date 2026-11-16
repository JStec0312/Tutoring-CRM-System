using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class GoogleAccountLinkConfiguration
    : IEntityTypeConfiguration<GoogleAccountLink>
{
    public void Configure(EntityTypeBuilder<GoogleAccountLink> builder)
    {
        builder.ToTable("GoogleAccountLinks");

        builder.HasKey(link => link.AccountId);

        builder.Property(link => link.AccountId)
            .HasConversion(id => id.Value, value => new UserAccountId(value))
            .ValueGeneratedNever();

        builder.Property(link => link.GoogleSubject)
            .HasMaxLength(255)
            .IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2")
            .IsRequired();

        builder.HasIndex(link => link.GoogleSubject).IsUnique();

        builder.HasOne<UserAccount>()
            .WithOne()
            .HasForeignKey<GoogleAccountLink>(link => link.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}