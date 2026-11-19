using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

public sealed class FounderAdminsTests(AuthFixture fixture) : IClassFixture<AuthFixture>
{
    [Fact]
    public async Task ProductionStartCreatesExactlyTwoActiveAdminsAndARestartAddsNoMore()
    {
        var first = (Email: $"{Guid.NewGuid():N}@ucb.edu.bo", Second: $"{Guid.NewGuid():N}@ucb.edu.bo");
        await StartAsync(first.Email, first.Second);
        await StartAsync($"{Guid.NewGuid():N}@ucb.edu.bo", $"{Guid.NewGuid():N}@ucb.edu.bo");

        await using var db = fixture.Context();
        var founders = await db.AuditEvents.Where(e => e.Action == AuditActions.FounderAdminProvisioned).Select(e => e.ResourceId).ToListAsync();
        Assert.Equal(2, founders.Count);
        var users = await db.Users.Where(u => founders.Contains(u.Id)).ToListAsync();
        Assert.Equal(new[] { first.Email, first.Second }.Order(), users.Select(u => u.Email!).Order());
        Assert.All(users, u => Assert.Equal(AccountStatus.ACTIVE, u.Status));
        Assert.Equal(2, await db.RoleAssignments.CountAsync(r =>
            founders.Contains(r.UserId) && r.Role == AssignedRole.ADMIN && r.Scope == RoleScope.GLOBAL && r.RevokedAt == null));
    }

    private async Task StartAsync(string firstEmail, string secondEmail)
    {
        await using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", fixture.ConnectionString);
            builder.UseSetting("JWT_SECRET", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
            builder.UseSetting("JWT_ISSUER", "auth-test");
            builder.UseSetting("JWT_AUDIENCE", "auth-test-client");
            builder.UseSetting("REDIS_CONNECTION_STRING", fixture.RedisConnection);
            builder.UseSetting("FOUNDER_ADMIN_1_EMAIL", firstEmail);
            builder.UseSetting("FOUNDER_ADMIN_1_PASSWORD", "Fundador-" + Guid.NewGuid().ToString("N"));
            builder.UseSetting("FOUNDER_ADMIN_2_EMAIL", secondEmail);
            builder.UseSetting("FOUNDER_ADMIN_2_PASSWORD", "Fundador-" + Guid.NewGuid().ToString("N"));
            builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(fixture.Mail));
        });
        using var client = host.CreateClient();
        await client.GetAsync("/health");
    }
}
