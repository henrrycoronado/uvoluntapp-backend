using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParticipationAndActivePeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activities_program_periods_program_id_period_id",
                table: "activities");

            migrationBuilder.Sql("""
                CREATE SCHEMA IF NOT EXISTS planning_history;
                ALTER TABLE program_periods SET SCHEMA planning_history;
                CREATE TABLE planning_history.activity_notes AS
                    SELECT id, periodicity_note FROM activities WHERE periodicity_note IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_activities_program_id_period_id",
                table: "activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_capacity",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "periodicity_note",
                table: "activities");

            migrationBuilder.AddColumn<bool>(
                name: "allow_multiple_groups",
                table: "activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_self_checkin",
                table: "activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "checkin_tolerance_min",
                table: "activities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "geofence_radius_m",
                table: "activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "activities",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "activities",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "academic_periods",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "academic_periods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "previous_period_id",
                table: "academic_periods",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    selected uuid;
                    today date := (CURRENT_TIMESTAMP AT TIME ZONE 'America/La_Paz')::date;
                BEGIN
                    IF (SELECT count(DISTINCT period_id) FROM activities WHERE deleted_at IS NULL AND status IN ('DRAFT','PUBLISHED','IN_PROGRESS')) > 1 THEN
                        RAISE EXCEPTION 'Hay actividades pendientes en varios periodos. Resolverlas antes de migrar.';
                    END IF;
                    SELECT id INTO selected FROM academic_periods p WHERE deleted_at IS NULL
                      ORDER BY EXISTS (SELECT 1 FROM activities a WHERE a.period_id = p.id AND a.deleted_at IS NULL AND a.status IN ('DRAFT','PUBLISHED','IN_PROGRESS')) DESC,
                          (start_date <= today AND end_date >= today) DESC, start_date DESC, id LIMIT 1;
                    IF selected IS NULL THEN
                        INSERT INTO academic_periods (id, label, start_date, created_at, is_active)
                            VALUES (gen_random_uuid(), to_char(today,'YYYY') || CASE WHEN extract(month from today) <= 6 THEN '-I' ELSE '-II' END, today, now(), true);
                    ELSE
                        UPDATE academic_periods SET is_active = true WHERE id = selected;
                    END IF;
                END $$;

                CREATE FUNCTION guard_activity_period() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'UPDATE' AND (OLD.period_id <> NEW.period_id OR OLD.program_id IS DISTINCT FROM NEW.program_id) THEN
                        RAISE EXCEPTION 'Programa y periodo inmutables' USING ERRCODE = '23514';
                    END IF;
                    IF (SELECT NOT is_active FROM academic_periods WHERE id = NEW.period_id FOR SHARE) THEN
                        RAISE EXCEPTION 'Periodo cerrado' USING ERRCODE = '23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER activity_period_guard BEFORE INSERT OR UPDATE ON activities
                    FOR EACH ROW EXECUTE FUNCTION guard_activity_period();
                """);

            migrationBuilder.CreateTable(
                name: "activity_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_groups", x => x.id);
                    table.UniqueConstraint("AK_activity_groups_activity_id_id", x => new { x.activity_id, x.id });
                    table.CheckConstraint("ck_activity_group_schedule", "ends_at > starts_at AND (capacity IS NULL OR capacity >= 0)");
                    table.ForeignKey(
                        name: "FK_activity_groups_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "FK_enrollments_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_activity_groups_activity_id_group_id",
                        columns: x => new { x.activity_id, x.group_id },
                        principalTable: "activity_groups",
                        principalColumns: new[] { "activity_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activities_program_id",
                table: "activities",
                column: "program_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_capacity",
                table: "activities",
                sql: "capacity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_academic_periods_is_active",
                table: "academic_periods",
                column: "is_active",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_academic_periods_previous_period_id",
                table: "academic_periods",
                column: "previous_period_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_activity_id_group_id",
                table: "enrollments",
                columns: new[] { "activity_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_activity_id_user_id",
                table: "enrollments",
                columns: new[] { "activity_id", "user_id" },
                unique: true,
                filter: "cancelled_at IS NULL AND group_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_group_id_user_id",
                table: "enrollments",
                columns: new[] { "group_id", "user_id" },
                unique: true,
                filter: "cancelled_at IS NULL AND group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_user_id",
                table: "enrollments",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_academic_periods_academic_periods_previous_period_id",
                table: "academic_periods",
                column: "previous_period_id",
                principalTable: "academic_periods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER activity_period_guard ON activities; DROP FUNCTION guard_activity_period();");

            migrationBuilder.DropForeignKey(
                name: "FK_academic_periods_academic_periods_previous_period_id",
                table: "academic_periods");

            migrationBuilder.DropTable(
                name: "enrollments");

            migrationBuilder.DropTable(
                name: "activity_groups");

            migrationBuilder.DropIndex(
                name: "IX_activities_program_id",
                table: "activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_capacity",
                table: "activities");

            migrationBuilder.DropIndex(
                name: "IX_academic_periods_is_active",
                table: "academic_periods");

            migrationBuilder.DropIndex(
                name: "IX_academic_periods_previous_period_id",
                table: "academic_periods");

            migrationBuilder.DropColumn(
                name: "allow_multiple_groups",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "allow_self_checkin",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "checkin_tolerance_min",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "geofence_radius_m",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "academic_periods");

            migrationBuilder.DropColumn(
                name: "previous_period_id",
                table: "academic_periods");

            migrationBuilder.AddColumn<string>(
                name: "periodicity_note",
                table: "activities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "academic_periods",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE planning_history.program_periods SET SCHEMA public;
                INSERT INTO program_periods (id, program_id, academic_period_id, created_at)
                    SELECT gen_random_uuid(), program_id, period_id, now() FROM
                    (SELECT DISTINCT program_id, period_id FROM activities WHERE program_id IS NOT NULL) links
                    ON CONFLICT (program_id, academic_period_id) DO NOTHING;
                UPDATE activities a SET periodicity_note = h.periodicity_note FROM planning_history.activity_notes h WHERE h.id = a.id;
                DROP TABLE planning_history.activity_notes;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_activities_program_id_period_id",
                table: "activities",
                columns: new[] { "program_id", "period_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_capacity",
                table: "activities",
                sql: "capacity > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_activities_program_periods_program_id_period_id",
                table: "activities",
                columns: new[] { "program_id", "period_id" },
                principalTable: "program_periods",
                principalColumns: new[] { "program_id", "academic_period_id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
