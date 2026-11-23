using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Activities;
using Uvoluntapp.Backend.Domain.Entities.Audit;
using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence.Records;

namespace Uvoluntapp.Backend.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Career> Careers => Set<Career>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public DbSet<AccountApprovalRequest> AccountApprovalRequests => Set<AccountApprovalRequest>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    public DbSet<AdminGrantRequest> AdminGrantRequests => Set<AdminGrantRequest>();

    public DbSet<AdminGrantApproval> AdminGrantApprovals => Set<AdminGrantApproval>();

    public DbSet<AcademicPeriod> AcademicPeriods => Set<AcademicPeriod>();

    public DbSet<VolunteerProgram> Programs => Set<VolunteerProgram>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<ActivityReschedule> ActivityReschedules => Set<ActivityReschedule>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<PasswordRecoveryToken> PasswordRecoveryTokens => Set<PasswordRecoveryToken>();

    public DbSet<PendingEmailVerificationRecord> PendingEmailVerifications => Set<PendingEmailVerificationRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Ignore<IdentityRole<Guid>>();
        model.Ignore<IdentityRoleClaim<Guid>>();
        model.Ignore<IdentityUserRole<Guid>>();
        model.Ignore<IdentityUserClaim<Guid>>();
        model.Ignore<IdentityUserToken<Guid>>();

        model.Entity<AppUser>(user =>
        {
            user.ToTable("users");
            user.Property(u => u.Status).HasColumnName("account_status");
            user.HasIndex(u => u.NormalizedEmail).IsUnique();
        });
        model.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        model.Entity<Career>().ToTable("careers");
        model.Entity<Profile>(profile =>
        {
            profile.ToTable("profiles");
            profile.HasOne<AppUser>().WithOne().HasForeignKey<Profile>(p => p.UserId);
            profile.HasOne<Career>().WithMany().HasForeignKey(p => p.CareerId);
        });
        model.Entity<AuthSession>(session =>
        {
            session.ToTable("auth_sessions");
            session.HasOne<AppUser>().WithMany().HasForeignKey(s => s.UserId);
            session.HasIndex(s => s.RefreshTokenHash).IsUnique();
        });
        model.Entity<PasswordRecoveryToken>(token =>
        {
            token.ToTable("password_recovery_tokens");
            token.HasKey(t => t.Id);
            token.Property(t => t.UsedAt).IsConcurrencyToken();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId);
        });
        model.Entity<PendingEmailVerificationRecord>(pending =>
        {
            pending.ToTable("pending_email_verifications");
            pending.HasKey(p => p.Id);
            pending.HasIndex(p => new { p.UserId, p.Purpose }).IsUnique();
            pending.HasOne<AppUser>().WithMany().HasForeignKey(p => p.UserId);
        });
        model.Entity<AccountApprovalRequest>(request =>
        {
            request.ToTable("account_approval_requests");
            request.Property(r => r.Relationship).HasColumnName("relation_with_pastoral");
            request.HasOne<AppUser>().WithMany().HasForeignKey(r => r.UserId);
            request.HasOne<AppUser>().WithMany().HasForeignKey(r => r.ReviewedBy);
        });
        model.Entity<AdminGrantRequest>(request =>
        {
            request.ToTable("admin_grant_requests");
            request.HasOne<AppUser>().WithMany().HasForeignKey(r => r.BeneficiaryId);
            request.HasOne<AppUser>().WithMany().HasForeignKey(r => r.RequestedBy);
        });
        model.Entity<AdminGrantApproval>(approval =>
        {
            approval.ToTable("admin_grant_approvals");
            approval.Property(a => a.AdminId).HasColumnName("approver_id");
            approval.HasOne<AdminGrantRequest>().WithMany().HasForeignKey(a => a.RequestId);
            approval.HasOne<AppUser>().WithMany().HasForeignKey(a => a.AdminId);
            approval.HasIndex(a => new { a.RequestId, a.AdminId }).IsUnique();
        });
        model.Entity<RoleAssignment>(role =>
        {
            role.ToTable("role_assignments", t => t.HasCheckConstraint(
                "ck_role_assignments_scope_id", "(scope = 'GLOBAL') = (scope_id IS NULL)"));
            role.HasOne<AppUser>().WithMany().HasForeignKey(r => r.UserId);
            role.HasOne<AppUser>().WithMany().HasForeignKey(r => r.GrantedBy);

            // Un scope_id tiene dos destinos posibles. PostgreSQL comprueba solo la FK del ámbito elegido.
            role.Property<Guid?>("scope_program_id")
                .HasComputedColumnSql("CASE WHEN scope = 'PROGRAM' THEN scope_id END", stored: true);
            role.Property<Guid?>("scope_activity_id")
                .HasComputedColumnSql("CASE WHEN scope = 'ACTIVITY' THEN scope_id END", stored: true);
            role.HasOne<VolunteerProgram>().WithMany().HasForeignKey("scope_program_id");
            role.HasOne<Activity>().WithMany().HasForeignKey("scope_activity_id");
        });

        model.Entity<AcademicPeriod>(period =>
        {
            period.ToTable("academic_periods");
            period.Property(p => p.StartsAt).HasColumnName("start_date");
            period.Property(p => p.EndsAt).HasColumnName("end_date");
            period.Property(p => p.Label).HasMaxLength(20);
            period.HasIndex(p => p.IsActive).IsUnique().HasFilter("is_active");
            period.HasOne<AcademicPeriod>().WithMany().HasForeignKey(p => p.PreviousPeriodId);
            period.HasIndex(p => p.PreviousPeriodId).IsUnique();
            period.HasIndex(p => p.Label).IsUnique();
        });
        model.Entity<VolunteerProgram>(program =>
        {
            program.ToTable("programs");
            program.Property(p => p.Name).HasMaxLength(VolunteerProgram.NameMaxLength);
            program.Property(p => p.Description).HasMaxLength(VolunteerProgram.DescriptionMaxLength);
            program.Property(p => p.Contact).HasMaxLength(VolunteerProgram.ContactMaxLength);
            program.Property(p => p.ReferenceSchedule).HasMaxLength(VolunteerProgram.ReferenceScheduleMaxLength);
        });
        model.Entity<Activity>(activity =>
        {
            activity.ToTable("activities", t =>
            {
                t.HasCheckConstraint("ck_activities_schedule", "ends_at > starts_at");
                t.HasCheckConstraint("ck_activities_capacity", "capacity >= 0");

                // El cupo solo existe si la actividad requiere registro previo (SCRUM-49).
                t.HasCheckConstraint("ck_activities_capacity_requires_registration", "capacity IS NULL OR requires_registration");
                t.HasCheckConstraint("ck_activities_expected_hours", "expected_hours IS NULL OR (expected_hours >= 0 AND expected_hours <= 1000)");
            });
            activity.Property(a => a.Title).HasMaxLength(Activity.TitleMaxLength);
            activity.Property(a => a.Description).HasMaxLength(Activity.TextMaxLength);
            activity.Property(a => a.Location).HasMaxLength(Activity.LocationMaxLength);
            activity.Property(a => a.MeetingUrl).HasMaxLength(Activity.UrlMaxLength);
            activity.Property(a => a.ParticipationConditions).HasMaxLength(Activity.TextMaxLength);
            activity.HasMany(a => a.Groups).WithOne().HasForeignKey(g => g.ActivityId).OnDelete(DeleteBehavior.Cascade);
            activity.Navigation(a => a.Groups).AutoInclude();
            activity.Property(a => a.GroupLabel).HasMaxLength(Activity.GroupLabelMaxLength);
            activity.Property(a => a.ExpectedHours).HasPrecision(6, 1);
            activity.HasIndex(a => new { a.Status, a.StartsAt });
            activity.Property(a => a.AcademicPeriodId).HasColumnName("period_id");
            activity.HasOne<AcademicPeriod>().WithMany().HasForeignKey(a => a.AcademicPeriodId);
            activity.HasOne<VolunteerProgram>().WithMany().HasForeignKey(a => a.ProgramId);
        });
        model.Entity<MediaAsset>(media =>
        {
            media.ToTable("media_assets", t => t.HasCheckConstraint("ck_media_owner", "num_nonnulls(user_id,program_id,activity_id) = 1"));
            media.HasQueryFilter(m => m.DeletedAt == null);
            media.HasOne<AppUser>().WithMany().HasForeignKey(m => m.UserId);
            media.HasOne<Activity>().WithMany(a => a.Images).HasForeignKey(m => m.ActivityId);
            media.HasOne<VolunteerProgram>().WithMany(p => p.Images).HasForeignKey(m => m.ProgramId);
            media.HasIndex(m => m.UserId).IsUnique().HasFilter("deleted_at IS NULL AND user_id IS NOT NULL");
            media.HasIndex(m => m.ProgramId).IsUnique().HasFilter("deleted_at IS NULL AND program_id IS NOT NULL");
            media.HasIndex(m => m.ActivityId).IsUnique().HasFilter("deleted_at IS NULL AND activity_id IS NOT NULL");
        });
        model.Entity<Activity>().Navigation(a => a.Images).AutoInclude();
        model.Entity<VolunteerProgram>().Navigation(p => p.Images).AutoInclude();
        model.Entity<Enrollment>(enrollment =>
        {
            enrollment.ToTable("enrollments");
            enrollment.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId);
            enrollment.HasOne<Activity>().WithMany().HasForeignKey(e => e.ActivityId);
            enrollment.HasOne<ActivityGroup>().WithMany().HasForeignKey(e => new { e.ActivityId, e.GroupId }).HasPrincipalKey(g => new { g.ActivityId, g.Id });
            enrollment.HasIndex(e => new { e.ActivityId, e.UserId }).IsUnique().HasFilter("cancelled_at IS NULL AND group_id IS NULL");
            enrollment.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique().HasFilter("cancelled_at IS NULL AND group_id IS NOT NULL");
        });
        model.Entity<ActivityGroup>(group =>
        {
            group.ToTable("activity_groups", t => t.HasCheckConstraint("ck_activity_group_schedule", "ends_at > starts_at AND (capacity IS NULL OR capacity >= 0)"));
            group.Property(g => g.Id).ValueGeneratedNever();
            group.HasAlternateKey(g => new { g.ActivityId, g.Id });
            group.Property(g => g.Name).HasMaxLength(100);
        });
        model.Entity<ActivityReschedule>(reschedule =>
        {
            reschedule.ToTable("activity_reschedules", t =>
            {
                t.HasCheckConstraint("ck_activity_reschedules_previous_schedule", "previous_ends_at > previous_starts_at");
                t.HasCheckConstraint("ck_activity_reschedules_new_schedule", "new_ends_at > new_starts_at");
            });
            reschedule.Property(r => r.AuthorId).HasColumnName("rescheduled_by");
            reschedule.HasOne<AppUser>().WithMany().HasForeignKey(r => r.AuthorId);
            reschedule.HasOne<Activity>().WithMany().HasForeignKey(r => r.ActivityId);
        });
        model.Entity<AuditEvent>(audit =>
        {
            audit.ToTable("audit_events");
            audit.HasOne<AppUser>().WithMany().HasForeignKey(e => e.ActorId);
            audit.Property(e => e.ResourceType).HasColumnName("entity_type");
            audit.Property(e => e.ResourceId).HasColumnName("entity_id");
            audit.Property(e => e.Data).HasColumnName("details").HasColumnType("jsonb");
        });

        ApplySchemaConventions(model);
    }

    private static void ApplySchemaConventions(ModelBuilder model)
    {
        foreach (var type in model.Model.GetEntityTypes())
        {
            var entity = model.Entity(type.ClrType);
            if (typeof(Entity).IsAssignableFrom(type.ClrType))
            {
                entity.Property<uint>("xmin").IsRowVersion();
            }

            foreach (var foreignKey in type.GetForeignKeys())
            {
                if (foreignKey.DeclaringEntityType.ClrType != typeof(ActivityGroup))
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }

            foreach (var property in type.GetProperties())
            {
                var column = JsonNamingPolicy.SnakeCaseLower.ConvertName(property.GetColumnName());
                property.SetColumnName(column);
                if (property.ClrType.IsEnum)
                {
                    var values = string.Join(", ", Enum.GetNames(property.ClrType).Select(name => $"'{name}'"));
                    entity.ToTable(t => t.HasCheckConstraint($"ck_{type.GetTableName()}_{column}", $"{column} IN ({values})"));
                }
            }
        }
    }
}
