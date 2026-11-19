using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Application.Accounts.Contracts;
using Uvoluntapp.Backend.Application.Accounts.Google;
using Uvoluntapp.Backend.Application.Accounts.Recovery;
using Uvoluntapp.Backend.Domain.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit;

public sealed class GoogleAndRecoveryTests
{
    [Fact]
    public async Task RejectsAnAudienceNotInTheConfiguredList()
    {
        var validator = new FakeGoogleValidator(new("sub", "a@example.com", true, "wrong", null));

        await Assert.ThrowsAsync<AccountException>(() =>
            new GoogleAuthService(validator, new FakeGoogleAccounts())
                .AuthenticateAsync(
                    new GoogleAuthRequest("token"),
                    new[] { "expected" }));
    }

    [Fact]
    public async Task RejectsAnUnverifiedGoogleEmail()
    {
        var validator = new FakeGoogleValidator(new("sub", "a@example.com", false, "expected", null));

        await Assert.ThrowsAsync<AccountException>(() =>
            new GoogleAuthService(validator, new FakeGoogleAccounts())
                .AuthenticateAsync(
                    new GoogleAuthRequest("token"),
                    new[] { "expected" }));
    }

    [Fact]
    public void AllowsLinkingOnlyWhenBothEmailsAreVerified()
    {
        var identity = new GoogleIdentity("sub", "a@example.com", true, null);

        var decision = GoogleAccountLinkingPolicy.Decide(identity, true, true);

        Assert.True(decision.LinkExisting);
        Assert.False(decision.IsNewAccount);
        Assert.Throws<DomainRuleViolationException>(() =>
            GoogleAccountLinkingPolicy.Decide(identity, true, false));
    }

    [Fact]
    public async Task FirstExternalLoginIsPendingApproval()
    {
        var accounts = new FakeGoogleAccounts();
        var result = await AuthenticateAsync(
            accounts,
            new("sub", "a@example.com", true, "expected", null));

        Assert.Equal(AccountStatus.PENDING_APPROVAL.ToString(), result.AccountStatus);
        Assert.True(result.IsNewAccount);
    }

    [Fact]
    public async Task InstitutionalHostedDomainIsActive()
    {
        var accounts = new FakeGoogleAccounts();
        var result = await AuthenticateAsync(
            accounts,
            new("sub", "a@ucb.edu.bo", true, "expected", "ucb.edu.bo"));

        Assert.Equal(AccountStatus.ACTIVE.ToString(), result.AccountStatus);
        Assert.True(accounts.CreatedInstitutionalEmailVerified);
    }

    [Fact]
    public void RecoveryTokenExpiresAfterThirtyMinutesAndCannotBeReused()
    {
        var issuedAt = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
        var token = PasswordRecoveryToken.Create(Guid.NewGuid(), "hash", issuedAt);

        Assert.False(token.TryConsume(issuedAt.AddMinutes(30)));
        var valid = PasswordRecoveryToken.Create(Guid.NewGuid(), "hash", issuedAt);
        Assert.True(valid.TryConsume(issuedAt.AddMinutes(1)));
        Assert.False(valid.TryConsume(issuedAt.AddMinutes(2)));
    }

    [Fact]
    public async Task RecoveryResponseIsIdenticalWhetherAccountExists()
    {
        var known = new FakeRecoveryAccounts(Guid.NewGuid());
        var unknown = new FakeRecoveryAccounts(null);
        var knownResponse = await RequestRecoveryAsync(known);
        var unknownResponse = await RequestRecoveryAsync(unknown);

        Assert.Equal(knownResponse.Message, unknownResponse.Message);
        Assert.Equal(1, known.SavedTokens);
        Assert.Equal(0, unknown.SavedTokens);
    }

    [Fact]
    public async Task InstitutionalGoogleAccountOpensASession()
    {
        var issuer = new FakeSessionIssuer();
        var response = await new GoogleAuthService(
                new FakeGoogleValidator(new GoogleTokenClaims("sub-1", "ana@ucb.edu.bo", true, "expected", "ucb.edu.bo")),
                new FakeGoogleAccounts(),
                issuer)
            .AuthenticateAsync(new GoogleAuthRequest("token"), new[] { "expected" });

        Assert.Equal("ACTIVE", response.AccountStatus);
        Assert.NotNull(response.Tokens);
        Assert.Equal(1, issuer.Issued);
    }

    [Fact]
    public async Task NewExternalGoogleAccountWaitsForApprovalWithoutSession()
    {
        var issuer = new FakeSessionIssuer();
        var response = await new GoogleAuthService(
                new FakeGoogleValidator(new GoogleTokenClaims("sub-2", "ana@gmail.com", true, "expected", null)),
                new FakeGoogleAccounts(),
                issuer)
            .AuthenticateAsync(new GoogleAuthRequest("token"), new[] { "expected" });

        Assert.Equal("PENDING_APPROVAL", response.AccountStatus);
        Assert.Null(response.Tokens);
        Assert.Equal(0, issuer.Issued);
    }

    [Fact]
    public async Task NewExternalGoogleAccountAsksForApprovalDataWithGoogleNames()
    {
        var response = await AuthenticateAsync(
            new FakeGoogleAccounts(),
            new("sub-3", "ana@gmail.com", true, "expected", null, "Ana", "Paz"));

        Assert.True(response.RequiresApprovalData);
        Assert.Equal("ana@gmail.com", response.Email);
        Assert.Equal(("Ana", "Paz"), (response.FirstName, response.LastName));
    }

    [Fact]
    public async Task InstitutionalGoogleAccountNeedsNoApprovalData()
    {
        var response = await AuthenticateAsync(
            new FakeGoogleAccounts(),
            new("sub-4", "ana@ucb.edu.bo", true, "expected", "ucb.edu.bo", "Ana", "Paz"));

        Assert.False(response.RequiresApprovalData);
        Assert.Null(response.FirstName);
    }

    private static Task<GoogleAuthResponse> AuthenticateAsync(
        FakeGoogleAccounts accounts,
        GoogleTokenClaims claims) =>
        new GoogleAuthService(new FakeGoogleValidator(claims), accounts)
            .AuthenticateAsync(new GoogleAuthRequest("token"), new[] { "expected" });

    private static Task<MessageResponse> RequestRecoveryAsync(FakeRecoveryAccounts accounts) =>
        new RecoveryService(
            accounts,
            new FakeRecoveryTokenGenerator(),
            new FakeRateLimiter(),
            new FakeEmailSender(),
            TimeProvider.System)
        .RequestAsync(new ForgotPasswordRequest("person@example.com"), "127.0.0.1");

    private sealed class FakeGoogleValidator(GoogleTokenClaims claims) : IGoogleTokenValidator
    {
        public Task<GoogleTokenClaims?> ValidateAsync(
            string idToken,
            IReadOnlyCollection<string> allowedAudiences,
            CancellationToken cancellationToken = default) => Task.FromResult<GoogleTokenClaims?>(claims);
    }

    private sealed class FakeSessionIssuer : ISessionIssuer
    {
        public int Issued { get; private set; }

        public Task<TokenPair> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            Issued++;
            return Task.FromResult(new TokenPair { AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1) });
        }
    }

    private sealed class FakeGoogleAccounts : IGoogleAccountStore
    {
        public bool CreatedInstitutionalEmailVerified { get; private set; }

        public Task<GoogleAccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<GoogleAccountRecord?>(null);

        public Task<GoogleAccountRecord?> FindBySubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult<GoogleAccountRecord?>(null);

        public Task<GoogleAccountRecord> CreateAsync(
            GoogleIdentity identity,
            AccountStatus status,
            bool institutionalEmailVerified,
            CancellationToken cancellationToken)
        {
            CreatedInstitutionalEmailVerified = institutionalEmailVerified;
            return Task.FromResult(new GoogleAccountRecord(Guid.NewGuid(), identity.Email, true, status));
        }

        public Task<bool> HasApprovalRequestAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task LinkAsync(Guid userId, GoogleIdentity identity, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeRecoveryAccounts(Guid? userId) : IRecoveryAccountStore
    {
        public int SavedTokens { get; private set; }

        public Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(userId);

        public Task SaveTokenAsync(PasswordRecoveryToken token, CancellationToken cancellationToken)
        {
            SavedTokens++;
            return Task.CompletedTask;
        }

        public Task<PasswordRecoveryToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult<PasswordRecoveryToken?>(null);

        public Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeRecoveryTokenGenerator : IRecoveryTokenGenerator
    {
        public (string RawToken, string TokenHash) Create() => ("raw", Hash("raw"));

        public string Hash(string rawToken) => "hash-" + rawToken;
    }

    private sealed class FakeRateLimiter : IAccountRateLimiter
    {
        public Task CheckAsync(string operation, string accountKey, string ipAddress, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
