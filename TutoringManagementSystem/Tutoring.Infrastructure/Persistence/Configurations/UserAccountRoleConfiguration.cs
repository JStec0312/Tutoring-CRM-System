using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountRoleConfiguration
    : IEntityTypeConfiguration<UserAccountRole>
{
    public void Configure(EntityTypeBuilder<UserAccountRole> builder)
    {
        builder.ToTable("UserAccountRoles");

        builder.HasKey(role => new
        {
            role.UserAccountId,
            role.Role
        });

        builder.Property(role => role.UserAccountId)
            .HasStronglyTypedId(value => new UserAccountId(value), "UserAccountId")
            .IsRequired();

        builder.Property(role => role.Role)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne<UserAccount>()
            .WithMany(account => account.RoleAssignments)
            .HasForeignKey(role => role.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
