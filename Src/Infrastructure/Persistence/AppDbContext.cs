using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using U_VoluntApp_Core.Src.Domain.Entities;
using CoreProgram = U_VoluntApp_Core.Src.Domain.Entities.Program;

namespace U_VoluntApp_Core.Src.Infrastructure.Persistence;

public sealed class AppUser : IdentityUser<Guid>
{
    public AccountStatus Status { get; set; } = AccountStatus.PRE_REGISTERED;

    public string? InstitutionalEmail { get; set; }

    public DateTime? InstitutionalEmailVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SuspendedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Career> Careers => Set<Career>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public DbSet<AccountApprovalRequest> AccountApprovalRequests => Set<AccountApprovalRequest>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    public DbSet<AdminGrantRequest> AdminGrantRequests => Set<AdminGrantRequest>();

    public DbSet<AdminGrantApproval> AdminGrantApprovals => Set<AdminGrantApproval>();

    public DbSet<AcademicPeriod> AcademicPeriods => Set<AcademicPeriod>();

    public DbSet<CoreProgram> Programs => Set<CoreProgram>();

    public DbSet<ProgramPeriod> ProgramPeriods => Set<ProgramPeriod>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<ActivityReschedule> ActivityReschedules => Set<ActivityReschedule>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

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
            role.HasOne<CoreProgram>().WithMany().HasForeignKey("scope_program_id");
            role.HasOne<Activity>().WithMany().HasForeignKey("scope_activity_id");
        });

        model.Entity<AcademicPeriod>(period =>
        {
            period.ToTable("academic_periods");
            period.Property(p => p.StartsAt).HasColumnName("start_date");
            period.Property(p => p.EndsAt).HasColumnName("end_date");
        });
        model.Entity<CoreProgram>().ToTable("programs");
        model.Entity<ProgramPeriod>(link =>
        {
            link.ToTable("program_periods");
            link.HasOne<CoreProgram>().WithMany().HasForeignKey(p => p.ProgramId);
            link.HasOne<AcademicPeriod>().WithMany().HasForeignKey(p => p.AcademicPeriodId);
        });
        model.Entity<Activity>(activity =>
        {
            activity.ToTable("activities", t =>
            {
                t.HasCheckConstraint("ck_activities_schedule", "ends_at > starts_at");
                t.HasCheckConstraint("ck_activities_capacity", "capacity > 0");
            });
            activity.Property(a => a.AcademicPeriodId).HasColumnName("period_id");
            activity.HasOne<AcademicPeriod>().WithMany().HasForeignKey(a => a.AcademicPeriodId);
            activity.HasOne<CoreProgram>().WithMany().HasForeignKey(a => a.ProgramId);
            activity.HasOne<ProgramPeriod>().WithMany()
                .HasForeignKey(a => new { a.ProgramId, a.AcademicPeriodId })
                .HasPrincipalKey(p => new { p.ProgramId, p.AcademicPeriodId });
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
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
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
