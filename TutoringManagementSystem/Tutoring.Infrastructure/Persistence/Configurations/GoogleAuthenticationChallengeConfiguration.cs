using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Infrastructure.Authentication.Google;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class GoogleAuthenticationChallengeConfiguration
    : IEntityTypeConfiguration<GoogleAuthenticationChallenge>
{
    public void Configure(EntityTypeBuilder<GoogleAuthenticationChallenge> builder)
    {
        builder.ToTable("GoogleAuthenticationChallenges");
        builder.HasKey(challenge => challenge.Nonce);
        builder.Property(challenge => challenge.Nonce)
            .HasMaxLength(64)
            .IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2")
            .ValueGeneratedNever();
        builder.Property(challenge => challenge.ExpiresAtUtc).IsRequired();
        builder.HasIndex(challenge => challenge.ExpiresAtUtc);
    }
}
