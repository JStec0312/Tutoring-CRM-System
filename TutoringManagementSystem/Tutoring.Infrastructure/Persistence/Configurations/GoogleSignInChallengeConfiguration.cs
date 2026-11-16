using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Infrastructure.Authentication.Google;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class GoogleSignInChallengeConfiguration
    : IEntityTypeConfiguration<GoogleSignInChallenge>
{
    public void Configure(EntityTypeBuilder<GoogleSignInChallenge> builder)
    {
        builder.ToTable("GoogleSignInChallenges");

        builder.HasKey(challenge => challenge.Nonce);

        builder.Property(challenge => challenge.Nonce)
            .HasMaxLength(64)
            .IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2");
    }
}