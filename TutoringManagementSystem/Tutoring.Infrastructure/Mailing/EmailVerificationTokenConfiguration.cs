using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class EmailVerificationTokenConfiguration
    : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(
        EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .ValueGeneratedNever();

        builder.Property(token => token.UserAccountId)
            .HasStronglyTypedId(
                value => new UserAccountId(value),
                "UserAccountId")
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(token => token.ExpiresAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(token => token.UsedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired(false);

        builder.Ignore(token => token.IsUsed);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName(
                "IX_EmailVerificationTokens_TokenHash");

        builder.HasIndex(token => token.UserAccountId)
            .HasDatabaseName(
                "IX_EmailVerificationTokens_UserAccountId");

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(token => token.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}