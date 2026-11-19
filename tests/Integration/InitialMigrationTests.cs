using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

namespace U_VoluntApp_Core.Tests.Integration;

public sealed class InitialMigrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly string[] ExpectedTables =
    [
        "users",
        "user_logins",
        "profiles",
        "careers",
        "auth_sessions",
        "account_approval_requests",
        "role_assignments",
        "admin_grant_requests",
        "admin_grant_approvals",
        "academic_periods",
        "programs",
        "program_periods",
        "activities",
        "activity_reschedules",
        "audit_events"
    ];

    [Fact]
    public async Task InitialCreateCreatesTheFifteenDomainTables()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name = ANY (@tableNames);
            """;
        command.Parameters.AddWithValue("tableNames", ExpectedTables);

        var actualTables = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            actualTables.Add(reader.GetString(0));
        }

        Assert.Equal(ExpectedTables.Length, actualTables.Count);
        Assert.All(ExpectedTables, tableName => Assert.Contains(tableName, actualTables));
    }
}
