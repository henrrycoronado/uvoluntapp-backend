using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration;

public sealed class InitialMigrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly string[] ExpectedTables =
    [
        "users", "user_logins", "profiles", "careers", "auth_sessions", "account_approval_requests",
        "role_assignments", "admin_grant_requests", "admin_grant_approvals", "academic_periods",
        "programs", "program_periods", "activities", "activity_reschedules", "audit_events"
    ];

    [Fact]
    public async Task InitialCreateCreatesExactlyTheFifteenDomainTables()
    {
        await using var context = fixture.CreateContext();
        var tables = await context.Database.SqlQueryRaw<string>("""
            SELECT table_name::text
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
              AND table_name <> '__EFMigrationsHistory'
            """).ToListAsync();

        Assert.Equal(ExpectedTables.Order(StringComparer.Ordinal), tables.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task AllDomainColumnsUseSnakeCase()
    {
        await using var context = fixture.CreateContext();
        var invalidColumns = await context.Database.SqlQueryRaw<string>("""
            SELECT table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
              AND column_name !~ '^[a-z][a-z0-9]*(_[a-z0-9]+)*$'
            """).ToListAsync();

        Assert.Empty(invalidColumns);
    }
}
