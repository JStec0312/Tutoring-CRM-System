using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonCancellationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAtUtc",
                table: "Lessons",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserAccountId",
                table: "Lessons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonCancellationParty",
                table: "Lessons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CancelledByUserAccountId",
                table: "Lessons",
                column: "CancelledByUserAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_UserAccounts_CancelledByUserAccountId",
                table: "Lessons",
                column: "CancelledByUserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_UserAccounts_CancelledByUserAccountId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CancelledByUserAccountId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CancelledByUserAccountId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LessonCancellationParty",
                table: "Lessons");
        }
    }
}
