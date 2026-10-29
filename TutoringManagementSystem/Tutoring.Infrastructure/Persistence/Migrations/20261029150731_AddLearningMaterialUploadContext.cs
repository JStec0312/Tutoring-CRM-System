using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningMaterialUploadContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy materials lack an unambiguous TA and authenticated uploader.
            // Require explicit data migration rather than inventing relationships or deleting data.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [LearningMaterials])
                    THROW 51000, 'LearningMaterials must be empty before this migration. Migrate legacy material context explicitly first.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId",
                table: "LearningMaterials");

            migrationBuilder.RenameColumn(
                name: "TutorId",
                table: "LearningMaterials",
                newName: "UploadedByUserAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_LearningMaterials_TutorId",
                table: "LearningMaterials",
                newName: "IX_LearningMaterials_UploadedByUserAccountId");

            migrationBuilder.AddColumn<Guid>(
                name: "LessonId",
                table: "LearningMaterials",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TutoringAgreementId",
                table: "LearningMaterials",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_LessonId",
                table: "LearningMaterials",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_TutoringAgreementId",
                table: "LearningMaterials",
                column: "TutoringAgreementId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_Lessons_LessonId",
                table: "LearningMaterials",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_TutoringAgreements_TutoringAgreementId",
                table: "LearningMaterials",
                column: "TutoringAgreementId",
                principalTable: "TutoringAgreements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_UserAccounts_UploadedByUserAccountId",
                table: "LearningMaterials",
                column: "UploadedByUserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [LearningMaterials])
                    THROW 51000, 'LearningMaterials must be empty before rollback. Student uploads cannot be represented by the legacy tutor-only model.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_Lessons_LessonId",
                table: "LearningMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_TutoringAgreements_TutoringAgreementId",
                table: "LearningMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_UserAccounts_UploadedByUserAccountId",
                table: "LearningMaterials");

            migrationBuilder.DropIndex(
                name: "IX_LearningMaterials_LessonId",
                table: "LearningMaterials");

            migrationBuilder.DropIndex(
                name: "IX_LearningMaterials_TutoringAgreementId",
                table: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "TutoringAgreementId",
                table: "LearningMaterials");

            migrationBuilder.RenameColumn(
                name: "UploadedByUserAccountId",
                table: "LearningMaterials",
                newName: "TutorId");

            migrationBuilder.RenameIndex(
                name: "IX_LearningMaterials_UploadedByUserAccountId",
                table: "LearningMaterials",
                newName: "IX_LearningMaterials_TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId",
                table: "LearningMaterials",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
