using System.Linq.Expressions;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Activities;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration;

public sealed class EnumPersistenceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task StatesRolesAndModalitiesRoundTripAsCanonicalText()
    {
        await using var db = fixture.CreateContext();
        var user = db.Users.Add(new AppUser()).Entity;
        var reviewer = db.Users.Add(new AppUser()).Entity;
        var approval = new AccountApprovalRequest { UserId = user.Id };
        var grant = new AdminGrantRequest { BeneficiaryId = user.Id, RequestedBy = reviewer.Id };
        var role = new RoleAssignment { UserId = user.Id, GrantedBy = reviewer.Id, Role = AssignedRole.ADMIN };
        var program = new VolunteerProgram();
        var period = new AcademicPeriod { Label = Guid.NewGuid().ToString("N")[..12] };
        var activity = new Activity
        {
            AcademicPeriodId = period.Id,
            Modality = ActivityModality.HYBRID,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1)
        };
        db.AddRange(approval, grant, role, program, period, activity);
        await db.SaveChangesAsync();

        await AssertRoundTrip(db, user, u => u.Status,
            "PRE_REGISTERED", "PENDING_VERIFICATION", "PENDING_APPROVAL", "ACTIVE", "REJECTED", "SUSPENDED");
        await AssertRoundTrip(db, approval, a => a.Status, "PENDING_APPROVAL", "ACTIVE", "REJECTED");
        await AssertRoundTrip(db, grant, g => g.Status, "PENDING", "APPROVED", "REJECTED");
        await AssertRoundTrip(db, role, r => r.Role, "ADMIN", "PROGRAM_LEADER", "OPERATIONAL_LEADER");
        await AssertRoundTrip(db, program, p => p.Status, "DRAFT", "ACTIVE", "INACTIVE");
        await AssertRoundTrip(db, activity, a => a.Status, "DRAFT", "PUBLISHED", "IN_PROGRESS", "FINISHED", "CANCELLED");
        await AssertRoundTrip(db, activity, a => a.Modality, "IN_PERSON", "VIRTUAL", "HYBRID");
    }

    [Theory]
    [InlineData(RoleScope.GLOBAL, "GLOBAL")]
    [InlineData(RoleScope.PROGRAM, "PROGRAM")]
    [InlineData(RoleScope.ACTIVITY, "ACTIVITY")]
    public async Task ScopeRoundTripsAsCanonicalText(RoleScope scope, string expected)
    {
        await using var db = fixture.CreateContext();
        var user = db.Users.Add(new AppUser()).Entity;
        var program = db.Programs.Add(new VolunteerProgram()).Entity;
        var period = db.AcademicPeriods.Add(new AcademicPeriod { Label = Guid.NewGuid().ToString("N")[..12] }).Entity;
        var activity = db.Activities.Add(new Activity
        {
            AcademicPeriodId = period.Id,
            Modality = ActivityModality.VIRTUAL,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1)
        }).Entity;

        var role = new RoleAssignment
        {
            UserId = user.Id,
            GrantedBy = user.Id,
            Role = scope == RoleScope.GLOBAL ? AssignedRole.ADMIN : AssignedRole.PROGRAM_LEADER,
            Scope = scope,
            ScopeId = scope switch { RoleScope.PROGRAM => program.Id, RoleScope.ACTIVITY => activity.Id, _ => null }
        };
        db.Add(role);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var stored = await db.Database.SqlQuery<string>($"SELECT scope FROM role_assignments WHERE id = {role.Id}").ToListAsync();
        Assert.Equal(expected, Assert.Single(stored));
        var reloaded = await db.RoleAssignments.SingleAsync(r => r.Id == role.Id);
        Assert.Equal(scope, reloaded.Scope);
        Assert.Equal(role.ScopeId, reloaded.ScopeId);
    }

    [Theory]
    [InlineData("VOLUNTEER")]
    [InlineData("ProgramLeader")]
    [InlineData("1")]
    public async Task DatabaseRejectsNonCanonicalRoles(string invalidRole)
    {
        await using var db = fixture.CreateContext();
        var user = db.Users.Add(new AppUser()).Entity;
        var role = db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = user.Id,
            GrantedBy = user.Id,
            Role = AssignedRole.ADMIN
        }).Entity;
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"UPDATE role_assignments SET role = {invalidRole} WHERE id = {role.Id}"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("ck_role_assignments_role", error.ConstraintName);
    }

    private static async Task AssertRoundTrip<TEntity, TEnum>(
        AppDbContext db, TEntity entity, Expression<Func<TEntity, TEnum>> selector, params string[] canonicalNames)
        where TEntity : class
        where TEnum : struct, Enum
    {
        Assert.Equal(canonicalNames.Order(), Enum.GetNames<TEnum>().Order());
        var entry = db.Entry(entity);
        var property = entry.Property(selector);
        var sql = $"SELECT {property.Metadata.GetColumnName()} FROM {entry.Metadata.GetTableName()} WHERE id = {{0}}";
        var id = entry.Property<Guid>("Id").CurrentValue;

        foreach (var name in canonicalNames)
        {
            var value = Enum.Parse<TEnum>(name);
            property.CurrentValue = value;
            await db.SaveChangesAsync();
            Assert.Equal(name, Assert.Single(await db.Database.SqlQueryRaw<string>(sql, id).ToListAsync()));
            await entry.ReloadAsync();
            Assert.Equal(value, property.CurrentValue);
        }
    }
}
