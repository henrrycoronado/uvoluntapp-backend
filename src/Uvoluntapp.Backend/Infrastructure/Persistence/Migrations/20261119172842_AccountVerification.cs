using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by",
                table: "users",
                type: "uuid",
                nullable: true);

            // Cuentas existentes: el correo institucional verificado, la aprobación de un ADMIN
            // o ser administrador cuentan como verificación.
            migrationBuilder.Sql("""
                UPDATE users SET verified_at = institutional_email_verified_at
                WHERE institutional_email_verified_at IS NOT NULL;

                UPDATE users u SET verified_at = r.reviewed_at, verified_by = r.reviewed_by
                FROM account_approval_requests r
                WHERE r.user_id = u.id AND r.status = 'ACTIVE' AND r.reviewed_at IS NOT NULL AND u.verified_at IS NULL;

                UPDATE users u SET verified_at = now()
                WHERE u.verified_at IS NULL AND EXISTS (
                    SELECT 1 FROM role_assignments a
                    WHERE a.user_id = u.id AND a.role = 'ADMIN' AND a.scope = 'GLOBAL' AND a.revoked_at IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "verified_by",
                table: "users");
        }
    }
}
