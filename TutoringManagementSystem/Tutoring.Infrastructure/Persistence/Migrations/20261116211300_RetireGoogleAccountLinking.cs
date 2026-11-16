using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutoring.Infrastructure.Persistence.Migrations
{
    public partial class RetireGoogleAccountLinking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the historical tables and data outside the active EF model.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The historical tables already exist.
        }
    }
}
