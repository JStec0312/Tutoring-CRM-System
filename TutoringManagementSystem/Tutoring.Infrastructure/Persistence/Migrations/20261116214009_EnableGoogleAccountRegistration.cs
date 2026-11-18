using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnableGoogleAccountRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UserAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.DropColumn(
                name: "IsEmailAuthoritative",
                table: "GoogleSignInContinuations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE PasswordHash IS NULL) THROW 51000, 'Cannot roll back while passwordless accounts exist.', 1;");

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailAuthoritative",
                table: "GoogleSignInContinuations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UserAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
