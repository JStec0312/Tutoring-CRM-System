using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentInvitationsAndAgreementComplexTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HourlyRateCurrencyCode",
                table: "TutoringAgreements",
                type: "char(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRateAmount",
                table: "TutoringAgreements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "AgreementTitle",
                table: "TutoringAgreements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HourlyRate_Discriminator",
                table: "TutoringAgreements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgreementTitle",
                table: "StudentInvitations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRateAmount",
                table: "StudentInvitations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HourlyRateCurrencyCode",
                table: "StudentInvitations",
                type: "char(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HourlyRate_Discriminator",
                table: "StudentInvitations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectName",
                table: "StudentInvitations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "StudentInvitations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StudentInvitations_TokenHash",
                table: "StudentInvitations",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentInvitations_TokenHash",
                table: "StudentInvitations");

            migrationBuilder.DropColumn(
                name: "AgreementTitle",
                table: "TutoringAgreements");

            migrationBuilder.DropColumn(
                name: "HourlyRate_Discriminator",
                table: "TutoringAgreements");

            migrationBuilder.DropColumn(
                name: "AgreementTitle",
                table: "StudentInvitations");

            migrationBuilder.DropColumn(
                name: "HourlyRateAmount",
                table: "StudentInvitations");

            migrationBuilder.DropColumn(
                name: "HourlyRateCurrencyCode",
                table: "StudentInvitations");

            migrationBuilder.DropColumn(
                name: "HourlyRate_Discriminator",
                table: "StudentInvitations");

            migrationBuilder.DropColumn(
                name: "SubjectName",
                table: "StudentInvitations");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "StudentInvitations");

            migrationBuilder.AlterColumn<string>(
                name: "HourlyRateCurrencyCode",
                table: "TutoringAgreements",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "char(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRateAmount",
                table: "TutoringAgreements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
