using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

public sealed class AccountVerificationTests(AuthFixture fixture) : IClassFixture<AuthFixture>
{
    [Fact]
    public async Task InstitutionalAccountIsVerifiedAndLosesItWhenTheAccessEmailBecomesExternal()
    {
        using var client = fixture.Client();
        var email = $"{Guid.NewGuid():N}@ucb.edu.bo";
        var password = Guid.NewGuid().ToString("N");
        (await client.PostAsJsonAsync("register", new { email, password, firstName = "Ana", lastName = "Paz" })).EnsureSuccessStatusCode();
        var verified = await (await client.PostAsJsonAsync("verify-otp", new { email, code = fixture.Mail.Code(email) }))
            .Content.ReadFromJsonAsync<VerifyOtpResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", verified!.Tokens!.AccessToken);
        Assert.NotEqual(JsonValueKind.Null, (await ProfileAsync(client)).GetProperty("verifiedAt").ValueKind);

        var gmail = $"{Guid.NewGuid():N}@gmail.com";
        (await client.PostAsJsonAsync("/api/v1/profile/change-email", new { newEmail = gmail, password })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/profile/verify-email-change", new { code = fixture.Mail.Code(gmail) })).EnsureSuccessStatusCode();

        await using var db = fixture.Context();
        var user = await db.Users.SingleAsync(u => u.Email == gmail);
        Assert.Null(user.VerifiedAt);
        Assert.Equal(email, user.InstitutionalEmail);
        Assert.Equal(AccountStatus.ACTIVE, user.Status);
        Assert.True(await db.AuditEvents.AnyAsync(e => e.Action == "ACCOUNT_UNVERIFIED" && e.ResourceId == user.Id));
        Assert.True((await client.PostAsJsonAsync("login", new { email = gmail, password })).IsSuccessStatusCode);
    }

    [Fact]
    public async Task AdminVerifiesManuallyAndReplacesALostAccessEmail()
    {
        using var admin = await AdminClientAsync();
        var (userId, oldEmail, password) = await ExternalActiveUserAsync();
        using var person = fixture.Client();
        var login = await (await person.PostAsJsonAsync("login", new { email = oldEmail, password })).Content.ReadFromJsonAsync<TokenResponse>();

        (await admin.PostAsync($"/api/v1/users/{userId}/verify", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/users/{userId}/verify", null)).StatusCode);
        var detail = await (await admin.GetAsync($"/api/v1/users/{userId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, detail.GetProperty("verifiedAt").ValueKind);

        var newEmail = $"{Guid.NewGuid():N}@gmail.com";
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"/api/v1/users/{userId}/change-email", new { newEmail, reason = "" })).StatusCode);
        (await admin.PostAsJsonAsync($"/api/v1/users/{userId}/change-email", new { newEmail, reason = "Egresado sin acceso al correo" })).EnsureSuccessStatusCode();

        Assert.False((await person.PostAsJsonAsync("refresh", new { refreshToken = login!.RefreshToken })).IsSuccessStatusCode);
        Assert.True((await person.PostAsJsonAsync("login", new { email = newEmail, password })).IsSuccessStatusCode);
        await using var db = fixture.Context();
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.Null(user.VerifiedAt);
        Assert.True(await db.AuditEvents.AnyAsync(e => e.Action == "ACCESS_EMAIL_REPLACED_BY_ADMIN" && e.ResourceId == userId));
    }

    private static async Task<JsonElement> ProfileAsync(HttpClient client) =>
        await (await client.GetAsync("/api/v1/profile")).Content.ReadFromJsonAsync<JsonElement>();

    private async Task<HttpClient> AdminClientAsync()
    {
        var (adminId, email, password) = await ExternalActiveUserAsync();
        await using (var db = fixture.Context())
        {
            db.RoleAssignments.Add(RoleAssignment.Assign(adminId, AssignedRole.ADMIN, RoleScope.GLOBAL, null, adminId));
            await db.SaveChangesAsync();
        }

        var client = fixture.Client();
        var tokens = await (await client.PostAsJsonAsync("login", new { email, password })).Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    private async Task<(Guid Id, string Email, string Password)> ExternalActiveUserAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.org";
        var password = Guid.NewGuid().ToString("N");
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            Status = AccountStatus.ACTIVE,
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        await using var db = fixture.Context();
        db.Users.Add(user);
        db.Profiles.Add(new Profile { UserId = user.Id, FirstName = "Ana", LastName = "Paz" });
        await db.SaveChangesAsync();
        return (user.Id, email, password);
    }
}
