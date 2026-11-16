using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleSignInSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleSignInAccountLinks",
                columns: table => new
                {
                    GoogleSubject = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleSignInAccountLinks", x => x.GoogleSubject);
                    table.ForeignKey(
                        name: "FK_GoogleSignInAccountLinks_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleSignInContinuations",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    GoogleSubject = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    IsEmailAuthoritative = table.Column<bool>(type: "bit", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleSignInContinuations", x => x.TokenHash);
                    table.ForeignKey(
                        name: "FK_GoogleSignInContinuations_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleSignInAccountLinks_AccountId",
                table: "GoogleSignInAccountLinks",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleSignInContinuations_AccountId",
                table: "GoogleSignInContinuations",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleSignInContinuations_ExpiresAtUtc",
                table: "GoogleSignInContinuations",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleSignInAccountLinks");

            migrationBuilder.DropTable(
                name: "GoogleSignInContinuations");
        }
    }
}
