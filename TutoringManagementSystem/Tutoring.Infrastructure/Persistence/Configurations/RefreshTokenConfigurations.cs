using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .ValueGeneratedNever();

        builder.Property(refreshToken => refreshToken.UserAccountId)
            .HasConversion(
                userAccountId => userAccountId.Value,
                value => new UserAccountId(value))
            .IsRequired();

        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.FamilyId)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.ExpiresAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.RevokedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(refreshToken => refreshToken.ReplacedByTokenId)
            .IsRequired(false);

        builder.Property(refreshToken => refreshToken.CreatedByIp)
            .HasColumnType("nvarchar(45)")
            .HasMaxLength(45)
            .IsRequired(false);
        
        builder.Property(refreshToken => refreshToken.CreatedByUserAgent)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired(false);
        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasIndex(refreshToken => refreshToken.UserAccountId)
            .HasDatabaseName("IX_RefreshTokens_UserAccountId");

        builder.HasIndex(refreshToken => refreshToken.FamilyId)
            .HasDatabaseName("IX_RefreshTokens_FamilyId");
        
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}