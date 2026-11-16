using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class GoogleSignInContinuationConfiguration : IEntityTypeConfiguration<GoogleSignInContinuation>
{
    public void Configure(EntityTypeBuilder<GoogleSignInContinuation> builder)
    {
        builder.ToTable("GoogleSignInContinuations");
        builder.HasKey(continuation => continuation.TokenHash);
        builder.Property(continuation => continuation.TokenHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2")
            .ValueGeneratedNever();
        builder.Property(continuation => continuation.GoogleSubject).IsRequired().HasMaxLength(255).IsUnicode(false);
        builder.Property(continuation => continuation.Email).IsRequired().HasMaxLength(320);
        builder.Property(continuation => continuation.IsEmailAuthoritative).IsRequired();
        builder.Property(continuation => continuation.AccountId)
            .HasConversion(id => id!.Value.Value, value => new UserAccountId(value));
        builder.Property(continuation => continuation.ExpiresAtUtc).IsRequired();
        builder.HasIndex(continuation => continuation.ExpiresAtUtc);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(continuation => continuation.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
