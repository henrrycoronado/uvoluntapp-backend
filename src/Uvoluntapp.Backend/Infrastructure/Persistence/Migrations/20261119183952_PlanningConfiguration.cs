using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanningConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "programs",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "programs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact",
                table: "programs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_schedule",
                table: "programs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "activities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "periodicity_note",
                table: "activities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "activities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "activities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "expected_hours",
                table: "activities",
                type: "numeric(6,1)",
                precision: 6,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_label",
                table: "activities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "meeting_url",
                table: "activities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "participation_conditions",
                table: "activities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_registration",
                table: "activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "label",
                table: "academic_periods",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_activities_status_starts_at",
                table: "activities",
                columns: new[] { "status", "starts_at" });

            // Las actividades existentes con cupo pasan a requerir registro previo, para cumplir la nueva regla.
            migrationBuilder.Sql("UPDATE activities SET requires_registration = true WHERE capacity IS NOT NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_capacity_requires_registration",
                table: "activities",
                sql: "capacity IS NULL OR requires_registration");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_expected_hours",
                table: "activities",
                sql: "expected_hours IS NULL OR (expected_hours >= 0 AND expected_hours <= 1000)");

            migrationBuilder.CreateIndex(
                name: "IX_academic_periods_label",
                table: "academic_periods",
                column: "label",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_activities_status_starts_at",
                table: "activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_capacity_requires_registration",
                table: "activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_expected_hours",
                table: "activities");

            migrationBuilder.DropIndex(
                name: "IX_academic_periods_label",
                table: "academic_periods");

            migrationBuilder.DropColumn(
                name: "contact",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "reference_schedule",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "expected_hours",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "group_label",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "meeting_url",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "participation_conditions",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "requires_registration",
                table: "activities");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "programs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "programs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "activities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "periodicity_note",
                table: "activities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "activities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "activities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "label",
                table: "academic_periods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
