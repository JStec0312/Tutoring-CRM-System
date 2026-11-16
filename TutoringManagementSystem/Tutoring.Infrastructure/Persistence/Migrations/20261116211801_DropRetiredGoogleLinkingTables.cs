using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    public partial class DropRetiredGoogleLinkingTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GoogleAccountLinks");
            migrationBuilder.DropTable(name: "GoogleSignInChallenges");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the historical schema. Dropped rows cannot be recovered here.
            migrationBuilder.CreateTable(
                name: "GoogleSignInChallenges",
                columns: table => new
                {
                    Nonce = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleSignInChallenges", x => x.Nonce);
                });

            migrationBuilder.CreateTable(
                name: "GoogleAccountLinks",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoogleSubject = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAccountLinks", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_GoogleAccountLinks_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAccountLinks_GoogleSubject",
                table: "GoogleAccountLinks",
                column: "GoogleSubject",
                unique: true);
        }
    }
}
