using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecoveryMailbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "password_recovery_tokens",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "password_recovery_tokens");
        }
    }
}
