using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonCostCalculation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TutoringAgreementId1",
                table: "BillingAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccounts_TutoringAgreementId1",
                table: "BillingAccounts",
                column: "TutoringAgreementId1",
                unique: true,
                filter: "[TutoringAgreementId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingAccounts_TutoringAgreements_TutoringAgreementId1",
                table: "BillingAccounts",
                column: "TutoringAgreementId1",
                principalTable: "TutoringAgreements",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingAccounts_TutoringAgreements_TutoringAgreementId1",
                table: "BillingAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BillingAccounts_TutoringAgreementId1",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "TutoringAgreementId1",
                table: "BillingAccounts");
        }
    }
}
