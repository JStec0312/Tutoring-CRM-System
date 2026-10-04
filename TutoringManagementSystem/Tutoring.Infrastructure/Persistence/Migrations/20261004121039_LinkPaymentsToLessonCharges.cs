using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkPaymentsToLessonCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "LessonCharges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [LessonCharges] WHERE [IsPaid] = 1)
                    THROW 51000, 'Cannot migrate paid lesson charges without an explicit payment-to-charge reconciliation.', 1;
                """);

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "LessonCharges");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "LessonCharges");

            migrationBuilder.CreateIndex(
                name: "IX_LessonCharges_PaymentId",
                table: "LessonCharges",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonCharges_Payments_PaymentId",
                table: "LessonCharges",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "LessonCharges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAtUtc",
                table: "LessonCharges",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE charges
                SET [IsPaid] = 1, [PaidAtUtc] = payments.[PaidAtUtc]
                FROM [LessonCharges] AS charges
                INNER JOIN [Payments] AS payments ON payments.[Id] = charges.[PaymentId];
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_LessonCharges_Payments_PaymentId",
                table: "LessonCharges");

            migrationBuilder.DropIndex(
                name: "IX_LessonCharges_PaymentId",
                table: "LessonCharges");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "LessonCharges");
        }
    }
}
