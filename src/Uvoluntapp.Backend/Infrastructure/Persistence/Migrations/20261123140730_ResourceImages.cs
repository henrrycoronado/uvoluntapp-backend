using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResourceImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    program_id = table.Column<Guid>(type: "uuid", nullable: true),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content_hash = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.id);
                    table.CheckConstraint("ck_media_owner", "num_nonnulls(user_id,program_id,activity_id) = 1");
                    table.ForeignKey(
                        name: "FK_media_assets_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_media_assets_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_media_assets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_activity_id",
                table: "media_assets",
                column: "activity_id",
                unique: true,
                filter: "deleted_at IS NULL AND activity_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_program_id",
                table: "media_assets",
                column: "program_id",
                unique: true,
                filter: "deleted_at IS NULL AND program_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_user_id",
                table: "media_assets",
                column: "user_id",
                unique: true,
                filter: "deleted_at IS NULL AND user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}
