using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using U_VoluntApp_Core.Src.Domain.Entities;
using U_VoluntApp_Core.Src.Infrastructure.Persistence;
using Xunit;
using CoreProgram = U_VoluntApp_Core.Src.Domain.Entities.Program;

namespace U_VoluntApp_Core.Tests.Integration;

public sealed class SchemaConstraintTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task RoleAssignmentRejectsOrphanUser()
    {
        await using var db = fixture.CreateContext();
        var grantor = db.Users.Add(new AppUser()).Entity;
        await db.SaveChangesAsync();
        db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = Guid.NewGuid(), GrantedBy = grantor.Id, Role = AssignedRole.ADMIN
        });

        await AssertViolation(db, PostgresErrorCodes.ForeignKeyViolation, "FK_role_assignments_users_user_id");
    }

    [Fact]
    public async Task IndependentActivityRejectsOrphanPeriod()
    {
        await using var db = fixture.CreateContext();
        db.Activities.Add(new Activity
        {
            AcademicPeriodId = Guid.NewGuid(), Modality = ActivityModality.VIRTUAL,
            StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(1)
        });

        await AssertViolation(db, PostgresErrorCodes.ForeignKeyViolation, "FK_activities_academic_periods_period_id");
    }

    [Fact]
    public async Task ProgramPeriodRejectsDuplicatePair()
    {
        await using var db = fixture.CreateContext();
        var program = db.Programs.Add(new CoreProgram()).Entity;
        var period = db.AcademicPeriods.Add(new AcademicPeriod()).Entity;
        db.ProgramPeriods.Add(new ProgramPeriod { ProgramId = program.Id, AcademicPeriodId = period.Id });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        db.ProgramPeriods.Add(new ProgramPeriod { ProgramId = program.Id, AcademicPeriodId = period.Id });
        await AssertViolation(db, PostgresErrorCodes.UniqueViolation, "AK_program_periods_program_id_academic_period_id");
    }

    [Fact]
    public async Task ActivityRequiresItsProgramToBeLinkedToItsPeriod()
    {
        await using var db = fixture.CreateContext();
        var period = db.AcademicPeriods.Add(new AcademicPeriod()).Entity;
        var program = db.Programs.Add(new CoreProgram()).Entity;
        var activity = db.Activities.Add(new Activity
        {
            AcademicPeriodId = period.Id, Modality = ActivityModality.VIRTUAL,
            StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(1)
        }).Entity;
        await db.SaveChangesAsync();

        activity.ProgramId = program.Id;
        await AssertViolation(db, PostgresErrorCodes.ForeignKeyViolation, "FK_activities_program_periods_program_id_period_id");

        db.ProgramPeriods.Add(new ProgramPeriod { ProgramId = program.Id, AcademicPeriodId = period.Id });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(program.Id, (await db.Activities.SingleAsync(a => a.Id == activity.Id)).ProgramId);
    }

    [Theory]
    [InlineData(RoleScope.GLOBAL, true)]
    [InlineData(RoleScope.PROGRAM, false)]
    [InlineData(RoleScope.ACTIVITY, false)]
    public async Task ScopeRequiresAnIdExactlyWhenNonGlobal(RoleScope scope, bool hasId)
    {
        await using var db = fixture.CreateContext();
        var user = db.Users.Add(new AppUser()).Entity;
        db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = user.Id, GrantedBy = user.Id, Role = AssignedRole.PROGRAM_LEADER,
            Scope = scope, ScopeId = hasId ? Guid.NewGuid() : null
        });

        await AssertViolation(db, PostgresErrorCodes.CheckViolation, "ck_role_assignments_scope_id");
    }

    [Theory]
    [InlineData(RoleScope.PROGRAM, "FK_role_assignments_programs_scope_program_id")]
    [InlineData(RoleScope.ACTIVITY, "FK_role_assignments_activities_scope_activity_id")]
    public async Task ScopedRoleRequiresAnExistingTarget(RoleScope scope, string constraint)
    {
        await using var db = fixture.CreateContext();
        var user = db.Users.Add(new AppUser()).Entity;
        db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = user.Id, GrantedBy = user.Id, Role = AssignedRole.PROGRAM_LEADER,
            Scope = scope, ScopeId = Guid.NewGuid()
        });

        await AssertViolation(db, PostgresErrorCodes.ForeignKeyViolation, constraint);
    }

    [Fact]
    public async Task GrantRequestRejectsASecondApprovalByTheSameAdmin()
    {
        await using var db = fixture.CreateContext();
        var admin = db.Users.Add(new AppUser()).Entity;
        var beneficiary = db.Users.Add(new AppUser()).Entity;
        var request = db.AdminGrantRequests.Add(new AdminGrantRequest
        {
            RequestedBy = admin.Id, BeneficiaryId = beneficiary.Id
        }).Entity;
        db.AdminGrantApprovals.Add(new AdminGrantApproval { RequestId = request.Id, AdminId = admin.Id });
        await db.SaveChangesAsync();

        db.AdminGrantApprovals.Add(new AdminGrantApproval { RequestId = request.Id, AdminId = admin.Id });
        await AssertViolation(db, PostgresErrorCodes.UniqueViolation, "IX_admin_grant_approvals_request_id_approver_id");
    }

    [Theory]
    [InlineData(0, 1, "ck_activities_schedule")]
    [InlineData(-1, 1, "ck_activities_schedule")]
    [InlineData(1, 0, "ck_activities_capacity")]
    public async Task ActivityRejectsInvalidScheduleOrCapacity(int hours, int capacity, string constraint)
    {
        await using var db = fixture.CreateContext();
        var period = db.AcademicPeriods.Add(new AcademicPeriod()).Entity;
        var start = DateTime.UtcNow;
        db.Activities.Add(new Activity
        {
            AcademicPeriodId = period.Id, Modality = ActivityModality.IN_PERSON,
            StartsAt = start, EndsAt = start.AddHours(hours), Capacity = capacity
        });

        await AssertViolation(db, PostgresErrorCodes.CheckViolation, constraint);
    }

    [Fact]
    public async Task UserCanOnlyHaveOneProfile()
    {
        await using var db = fixture.CreateContext();
        var user = db.Users.Add(new AppUser()).Entity;
        db.Profiles.Add(new Profile { UserId = user.Id });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        db.Profiles.Add(new Profile { UserId = user.Id });
        await AssertViolation(db, PostgresErrorCodes.UniqueViolation, "IX_profiles_user_id");
    }

    [Fact]
    public async Task AccountsCannotShareANormalizedEmail()
    {
        await using var db = fixture.CreateContext();
        db.Users.AddRange(
            new AppUser { Email = "test@example.org", NormalizedEmail = "TEST@EXAMPLE.ORG" },
            new AppUser { Email = "Test@example.org", NormalizedEmail = "TEST@EXAMPLE.ORG" });

        await AssertViolation(db, PostgresErrorCodes.UniqueViolation, "EmailIndex");
    }

    [Fact]
    public async Task SessionsCannotShareARefreshTokenHash()
    {
        await using var db = fixture.CreateContext();
        var user = db.Users.Add(new AppUser()).Entity;
        var hash = Guid.NewGuid().ToString();
        db.AuthSessions.AddRange(
            new AuthSession { UserId = user.Id, RefreshTokenHash = hash },
            new AuthSession { UserId = user.Id, RefreshTokenHash = hash });

        await AssertViolation(db, PostgresErrorCodes.UniqueViolation, "IX_auth_sessions_refresh_token_hash");
    }

    [Theory]
    [InlineData(0, 1, "ck_activity_reschedules_previous_schedule")]
    [InlineData(1, -1, "ck_activity_reschedules_new_schedule")]
    public async Task RescheduleRequiresTwoValidIntervals(int previousHours, int newHours, string constraint)
    {
        await using var db = fixture.CreateContext();
        var start = DateTime.UtcNow;
        var user = db.Users.Add(new AppUser()).Entity;
        var period = db.AcademicPeriods.Add(new AcademicPeriod()).Entity;
        var activity = db.Activities.Add(new Activity
        {
            AcademicPeriodId = period.Id, Modality = ActivityModality.IN_PERSON,
            StartsAt = start, EndsAt = start.AddHours(1)
        }).Entity;
        db.ActivityReschedules.Add(new ActivityReschedule
        {
            ActivityId = activity.Id, AuthorId = user.Id, Reason = "Cambio de horario",
            PreviousStartsAt = start, PreviousEndsAt = start.AddHours(previousHours),
            NewStartsAt = start.AddDays(1), NewEndsAt = start.AddDays(1).AddHours(newHours)
        });

        await AssertViolation(db, PostgresErrorCodes.CheckViolation, constraint);
    }

    private static async Task AssertViolation(AppDbContext db, string sqlState, string constraint)
    {
        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var error = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(sqlState, error.SqlState);
        Assert.Equal(constraint, error.ConstraintName);
    }
}
