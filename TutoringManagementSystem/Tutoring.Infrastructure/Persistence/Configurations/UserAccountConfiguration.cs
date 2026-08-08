using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration
    : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasGeneratedStronglyTypedId(value => new UserAccountId(value));

        builder.OwnsOne(account => account.Email, email =>
        {
            email.Property(valueObject => valueObject.Value)
                .HasColumnName("Email")
                .HasColumnType("nvarchar(320)")
                .HasMaxLength(320)
                .IsRequired();

            email.HasIndex(valueObject => valueObject.Value)
                .IsUnique()
                .HasDatabaseName("IX_UserAccounts_Email");
        });

        builder.OwnsOne(account => account.PasswordHash, passwordHash =>
        {
            passwordHash.Property(valueObject => valueObject.Value)
                .HasColumnName("PasswordHash")
                .HasColumnType("nvarchar(256)")
                .HasMaxLength(256)
                .IsRequired();
        });

        builder.Navigation(account => account.Email)
            .IsRequired();
        builder.Navigation(account => account.PasswordHash)
            .IsRequired();  
        

        builder.OwnsOne(account => account.Profile, profile =>
        {
            profile.Property(valueObject => valueObject.UserName)
                .HasColumnName("UserName")
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .IsRequired();
            profile.Property(valueObject => valueObject.FirstName)
                .HasColumnName("FirstName")
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .IsRequired(false);

            profile.Property(valueObject => valueObject.LastName)
                .HasColumnName("LastName")
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .IsRequired(false);

            profile.Property(valueObject => valueObject.PhoneNumber)
                .HasConversion(
                    phoneNumber => phoneNumber == null ? null : phoneNumber.Value,
                    value => value == null ? null : new PhoneNumber(value))
                .HasColumnName("PhoneNumber")
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30)
                .IsRequired(false);
        });

            

        builder.Navigation(account => account.Profile)
            .IsRequired();

        builder.Property(account => account.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(account => account.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Ignore(account => account.Roles);

        builder.HasMany(account => account.RoleAssignments)
            .WithOne()
            .HasForeignKey(role => role.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(account => account.RoleAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
