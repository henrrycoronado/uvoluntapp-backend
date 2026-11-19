using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Xunit;

using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Auth;
namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

public sealed class AuthFlowTests(AuthFixture fixture) : IClassFixture<AuthFixture>
{
    [Fact]
    public async Task InstitutionalRegistrationCreatesOneSessionAndAUsedCodeCannotBeReplayed()
    {
        using var client = fixture.Client();
        var request = NewRegistration("UCB.EDU.BO");
        var registered = await RegisterAsync(client, request);
        await using var db = fixture.Context();
        var user = await db.Users.SingleAsync(u => u.Email == request.Email);
        Assert.Equal(AccountStatus.PENDING_VERIFICATION, user.Status);
        var results = await Task.WhenAll(VerifyAsync(client, request.Email), VerifyAsync(client, request.Email));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        var success = Assert.Single(results, r => r.IsSuccessStatusCode);
        var verified = await success.Content.ReadFromJsonAsync<VerifyOtpResponse>();
        Assert.Equal("ACTIVE", verified!.AccountStatus);
        Assert.False(verified.RequiresApprovalData);
        Assert.NotNull(verified.Tokens);
        await db.Entry(user).ReloadAsync();
        Assert.True(user.EmailConfirmed);
        Assert.Equal(AccountStatus.ACTIVE, user.Status);
        Assert.Equal(request.Email, user.InstitutionalEmail);
        Assert.NotNull(user.InstitutionalEmailVerifiedAt);
        var session = await db.AuthSessions.SingleAsync(s => s.UserId == user.Id);
        Assert.Equal(SessionTokens.Hash(verified.Tokens.RefreshToken), session.RefreshTokenHash);
        Assert.Equal(registered, await RegisterAsync(client, request with { Password = Guid.NewGuid().ToString() }));
        Assert.True((await client.PostAsJsonAsync("login", new { request.Email, request.Password })).IsSuccessStatusCode);
    }

    [Fact]
    public async Task ExternalRegistrationRequiresMailboxProofAndCreatesOneApprovalWithAllFields()
    {
        using var client = fixture.Client();
        var request = NewRegistration("example.org");
        await RegisterAsync(client, request);
        var verified = await VerifyAsync(client, request.Email);
        verified.EnsureSuccessStatusCode();
        var json = await verified.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PENDING_APPROVAL", json.GetProperty("accountStatus").GetString());
        Assert.True(json.GetProperty("requiresApprovalData").GetBoolean());
        Assert.False(json.TryGetProperty("tokens", out _));
        var profile = new { request.Email, firstName = "Elena", lastName = "Pérez", phone = "79000000", relationWithPastoral = "Voluntaria externa" };
        await AssertProblemAsync(await client.PostAsJsonAsync("complete-registration", profile), HttpStatusCode.Conflict);
        var cookie = Assert.Single(verified.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        var completions = await Task.WhenAll(client.PostAsJsonAsync("complete-registration", profile), client.PostAsJsonAsync("complete-registration", profile));
        Assert.Single(completions, r => r.IsSuccessStatusCode);
        Assert.Single(completions, r => r.StatusCode == HttpStatusCode.Conflict);
        await AssertProblemAsync(await client.PostAsJsonAsync("login", new { request.Email, request.Password }), HttpStatusCode.UnprocessableEntity);
        await using var db = fixture.Context();
        var user = await db.Users.SingleAsync(u => u.Email == request.Email);
        var approval = await db.AccountApprovalRequests.SingleAsync(r => r.UserId == user.Id);
        Assert.Equal(AccountStatus.PENDING_APPROVAL, user.Status);
        Assert.Equal((profile.firstName, profile.lastName, profile.phone, profile.relationWithPastoral),
            (approval.FirstName, approval.LastName, approval.Phone, approval.Relationship));
        Assert.Empty(await db.AuthSessions.Where(s => s.UserId == user.Id).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentRefreshHasOneWinnerAndLogoutRevokesOnlyItsSession()
    {
        using var client = fixture.Client();
        var request = NewRegistration("ucb.edu.bo");
        await RegisterAsync(client, request);
        var signup = (await (await VerifyAsync(client, request.Email)).Content.ReadFromJsonAsync<VerifyOtpResponse>())!.Tokens!;
        var login = await client.PostAsJsonAsync("login", new { request.Email, request.Password });
        var original = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("refresh", new { original.RefreshToken }),
            client.PostAsJsonAsync("refresh", new { original.RefreshToken }));
        var winner = Assert.Single(responses, r => r.IsSuccessStatusCode);
        await AssertProblemAsync(Assert.Single(responses, r => !r.IsSuccessStatusCode), HttpStatusCode.Unauthorized);
        var updated = (await winner.Content.ReadFromJsonAsync<TokenResponse>())!;
        Assert.NotEqual(original.AccessToken, updated.AccessToken);
        Assert.NotEqual(original.RefreshToken, updated.RefreshToken);
        await AssertProblemAsync(await client.PostAsJsonAsync("refresh", new { original.RefreshToken }), HttpStatusCode.Unauthorized);
        await AssertProblemAsync(await client.PostAsJsonAsync("logout", new { updated.RefreshToken }), HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);
        await AssertProblemAsync(await client.PostAsJsonAsync("logout", new { updated.RefreshToken }), HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", updated.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("logout", new { updated.RefreshToken })).StatusCode);
        await AssertProblemAsync(await client.PostAsJsonAsync("refresh", new { updated.RefreshToken }), HttpStatusCode.Unauthorized);
        await AssertProblemAsync(await client.PostAsJsonAsync("logout", new { updated.RefreshToken }), HttpStatusCode.Unauthorized);
        Assert.True((await client.PostAsJsonAsync("refresh", new { signup.RefreshToken })).IsSuccessStatusCode);
        await using var db = fixture.Context();
        var stored = await db.AuthSessions.SingleAsync(s => s.RefreshTokenHash == SessionTokens.Hash(updated.RefreshToken));
        Assert.NotNull(stored.RevokedAt);
    }

    [Fact]
    public async Task FiveWrongOtpsExhaustTheCodeAndResendStartsANewChallenge()
    {
        using var client = fixture.Client();
        var request = NewRegistration("example.org");
        await RegisterAsync(client, request);
        var old = fixture.Mail.Code(request.Email);
        var wrong = old == "000000" ? "000001" : "000000";
        var failures = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.PostAsJsonAsync("verify-otp", new { request.Email, code = wrong })));
        Assert.All(failures, r => Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode));
        await AssertProblemAsync(await VerifyAsync(client, request.Email), HttpStatusCode.TooManyRequests);
        var resend = await client.PostAsJsonAsync("resend-otp", new { request.Email });
        var unknown = await client.PostAsJsonAsync("resend-otp", new { email = NewRegistration("example.org").Email });
        Assert.Equal(await resend.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.True((await VerifyAsync(client, request.Email)).IsSuccessStatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SixthLoginIsLimitedIndependentlyByEmailAndIp(bool commonEmail)
    {
        using var shared = fixture.Client();
        var email = NewRegistration("example.net").Email;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            using var unique = fixture.Client();
            var client = commonEmail ? unique : shared;
            var address = commonEmail ? (attempt % 2 == 0 ? email : email.ToUpperInvariant()) : NewRegistration("example.net").Email;
            var result = await client.PostAsJsonAsync("login", new { email = address, password = "invalid" });
            await AssertProblemAsync(result, attempt == 5 ? HttpStatusCode.TooManyRequests : HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task FailedLoginsAccumulateInIdentityAndLockOutTheAccount()
    {
        using var client = fixture.Client();
        var request = NewRegistration("ucb.edu.bo");
        await RegisterAsync(client, request);
        (await VerifyAsync(client, request.Email)).EnsureSuccessStatusCode();
        var bad = new { request.Email, password = "incorrect" };
        await AssertProblemAsync(await client.PostAsJsonAsync("login", bad), HttpStatusCode.Unauthorized);
        await using var db = fixture.Context();
        var user = await db.Users.SingleAsync(u => u.Email == request.Email);
        Assert.Equal(1, user.AccessFailedCount);
        for (var i = 1; i < 5; i++)
        {
            await AssertProblemAsync(await client.PostAsJsonAsync("login", bad), HttpStatusCode.Unauthorized);
        }

        await db.Entry(user).ReloadAsync();
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ExpiredOrSuspendedSessionsCannotBeRenewed()
    {
        using var client = fixture.Client();
        var request = NewRegistration("ucb.edu.bo");
        await RegisterAsync(client, request);
        var signup = (await (await VerifyAsync(client, request.Email)).Content.ReadFromJsonAsync<VerifyOtpResponse>())!.Tokens!;
        await using var db = fixture.Context();
        var hash = SessionTokens.Hash(signup.RefreshToken);
        var session = await db.AuthSessions.SingleAsync(s => s.RefreshTokenHash == hash);
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        await AssertProblemAsync(await client.PostAsJsonAsync("refresh", new { signup.RefreshToken }), HttpStatusCode.Unauthorized);

        var login = await client.PostAsJsonAsync("login", new { request.Email, request.Password });
        var second = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;
        var user = await db.Users.SingleAsync(u => u.Email == request.Email);
        user.Status = AccountStatus.SUSPENDED;
        await db.SaveChangesAsync();
        await AssertProblemAsync(await client.PostAsJsonAsync("refresh", new { second.RefreshToken }), HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.AccessToken);
        await AssertProblemAsync(await client.PostAsJsonAsync("logout", new { second.RefreshToken }), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OtpExpiresInTenMinutesAndUnknownAddressesHaveTheSameFailureBudget()
    {
        using var client = fixture.Client();
        var request = NewRegistration("example.org");
        await RegisterAsync(client, request);
        var key = "uvoluntapp:auth:otp:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Email.ToUpperInvariant())));
        var redis = fixture.Redis.GetDatabase();
        var ttl = await redis.KeyTimeToLiveAsync(key);
        Assert.InRange(ttl!.Value, TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(10));
        await redis.HashSetAsync(key, "issued", DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds());
        await AssertProblemAsync(await VerifyAsync(client, request.Email), HttpStatusCode.UnprocessableEntity);

        var unknown = NewRegistration("example.org").Email;
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            var response = await client.PostAsJsonAsync("verify-otp", new { email = unknown, code = "123456" });
            await AssertProblemAsync(response, attempt == 6 ? HttpStatusCode.TooManyRequests : HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task RateWindowAdmitsAttemptsAgainAfterExpiry()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AUTH_RATE_WINDOW_SECONDS"] = "1"
        }).Build();
        var limiter = new AccountRateLimiter(fixture.Redis, configuration);
        var email = NewRegistration("example.net").Email;
        var ip = new IPAddress(Guid.NewGuid().ToByteArray()).ToString();
        var attempts = Enumerable.Range(0, 8).Select(async _ =>
        {
            try
            {
                await limiter.CheckAsync("login", email, ip);
                return true;
            }
            catch (AccountException ex) when (ex.Error == AccountError.TooManyAttempts)
            {
                return false;
            }
        });
        Assert.Equal(5, (await Task.WhenAll(attempts)).Count(allowed => allowed));
        await Task.Delay(TimeSpan.FromMilliseconds(1050));
        await limiter.CheckAsync("login", email, ip);
    }

    [Fact]
    public async Task InvalidBodyReturnsValidationProblemDetails()
    {
        using var client = fixture.Client();
        await AssertProblemAsync(await client.PostAsJsonAsync("register", new { email = "invalid" }), HttpStatusCode.BadRequest);
        await AssertProblemAsync(await client.PostAsJsonAsync("verify-otp", new { email = "test@example.org", code = "00000" }), HttpStatusCode.BadRequest);
        await AssertProblemAsync(await client.PostAsJsonAsync("verify-otp", new { email = "test@example.org", code = "１２３４５６" }), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GoogleAccountWithoutPasswordProvesItsMailboxOnceToCreateOne()
    {
        using var client = fixture.Client();
        var email = await GoogleOnlyUserAsync(AccountStatus.ACTIVE);
        var password = Guid.NewGuid().ToString("N");

        var login = await client.PostAsJsonAsync("login", new { email, password });
        Assert.Equal(HttpStatusCode.Conflict, login.StatusCode);
        var problem = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("urn:uvoluntapp:password-setup-required", problem.GetProperty("type").GetString());
        var code = fixture.Mail.Code(email);

        var wrong = code == "000000" ? "111111" : "000000";
        await AssertProblemAsync(await client.PostAsJsonAsync("set-password", new { email, code = wrong, password }), HttpStatusCode.UnprocessableEntity);
        var set = await client.PostAsJsonAsync("set-password", new { email, code, password });
        set.EnsureSuccessStatusCode();
        var result = await set.Content.ReadFromJsonAsync<SetPasswordResponse>();
        Assert.Equal("ACTIVE", result!.AccountStatus);
        Assert.NotNull(result.Tokens);

        await AssertProblemAsync(await client.PostAsJsonAsync("set-password", new { email, code, password = "otra-clave-123" }), HttpStatusCode.UnprocessableEntity);
        Assert.True((await client.PostAsJsonAsync("login", new { email, password })).IsSuccessStatusCode);
    }

    [Fact]
    public async Task PendingGoogleAccountCreatesItsPasswordWithoutASession()
    {
        using var client = fixture.Client();
        var email = await GoogleOnlyUserAsync(AccountStatus.PENDING_APPROVAL);
        var password = Guid.NewGuid().ToString("N");
        await AssertProblemAsync(await client.PostAsJsonAsync("login", new { email, password }), HttpStatusCode.Conflict);

        var set = await client.PostAsJsonAsync("set-password", new { email, code = fixture.Mail.Code(email), password });
        set.EnsureSuccessStatusCode();
        var result = await set.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PENDING_APPROVAL", result.GetProperty("accountStatus").GetString());
        Assert.False(result.TryGetProperty("tokens", out _));
        await AssertProblemAsync(await client.PostAsJsonAsync("login", new { email, password }), HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PasswordResetLinkWorksOnceRevokesEverySessionAndIsAudited()
    {
        using var client = fixture.Client();
        var request = NewRegistration("ucb.edu.bo");
        await RegisterAsync(client, request);
        var verified = await (await VerifyAsync(client, request.Email)).Content.ReadFromJsonAsync<VerifyOtpResponse>();

        (await client.PostAsJsonAsync("forgot-password", new { email = request.Email })).EnsureSuccessStatusCode();
        var link = System.Text.RegularExpressions.Regex.Match(fixture.Mail.Message(request.Email), @"https?://\S+/restablecer\?token=(\S+)");
        Assert.True(link.Success);
        var token = Uri.UnescapeDataString(link.Groups[1].Value);

        await AssertProblemAsync(await client.PostAsJsonAsync("reset-password", new { token, newPassword = "corta" }), HttpStatusCode.BadRequest);
        var password = Guid.NewGuid().ToString("N");
        (await client.PostAsJsonAsync("reset-password", new { token, newPassword = password })).EnsureSuccessStatusCode();
        await AssertProblemAsync(await client.PostAsJsonAsync("reset-password", new { token, newPassword = password }), HttpStatusCode.UnprocessableEntity);

        Assert.False((await client.PostAsJsonAsync("refresh", new { refreshToken = verified!.Tokens!.RefreshToken })).IsSuccessStatusCode);
        Assert.False((await client.PostAsJsonAsync("login", new { request.Email, request.Password })).IsSuccessStatusCode);
        Assert.True((await client.PostAsJsonAsync("login", new { request.Email, password })).IsSuccessStatusCode);
        await using var db = fixture.Context();
        var user = await db.Users.SingleAsync(u => u.Email == request.Email);
        Assert.True(await db.AuditEvents.AnyAsync(e => e.Action == "PASSWORD_RESET" && e.ResourceId == user.Id));
    }

    private async Task<string> GoogleOnlyUserAsync(AccountStatus status)
    {
        var email = $"{Guid.NewGuid():N}@example.org";
        await using var db = fixture.Context();
        db.Users.Add(new Uvoluntapp.Backend.Infrastructure.Identity.AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            Status = status,
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        await db.SaveChangesAsync();
        return email;
    }

    private static RegisterRequest NewRegistration(string domain) => new()
    {
        Email = $"{Guid.NewGuid():N}@{domain}",
        Password = Guid.NewGuid().ToString("N"),
        FirstName = "Ana",
        LastName = "Paz"
    };

    private static async Task<RegisterResponse> RegisterAsync(HttpClient client, RegisterRequest request)
    {
        using var response = await client.PostAsJsonAsync("register", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    private Task<HttpResponseMessage> VerifyAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("verify-otp", new { email, code = fixture.Mail.Code(email) });

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expected, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("type").GetString()));
    }
}
