using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class GoogleSignInAccountLinkConfiguration : IEntityTypeConfiguration<GoogleSignInAccountLink>
{
    public void Configure(EntityTypeBuilder<GoogleSignInAccountLink> builder)
    {
        builder.ToTable("GoogleSignInAccountLinks");
        builder.HasKey(link => link.GoogleSubject);
        builder.Property(link => link.GoogleSubject)
            .HasMaxLength(255)
            .IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2")
            .ValueGeneratedNever();
        builder.Property(link => link.AccountId)
            .HasConversion(id => id.Value, value => new UserAccountId(value));
        builder.HasIndex(link => link.AccountId).IsUnique();
        builder.HasOne<UserAccount>().WithOne().HasForeignKey<GoogleSignInAccountLink>(link => link.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
