using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Application.Accounts.Contracts;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Accounts;

public sealed class AccountSessions(
    AppDbContext db,
    UserManager<AppUser> users,
    AccountRateLimiter limits,
    SessionTokens tokens,
    TimeProvider clock) : IAccountSessions
{
    public async Task<TokenResponse> LoginAsync(LoginRequest request, string ip, CancellationToken cancellationToken)
    {
        var email = users.NormalizeEmail(request.Email.Trim())!;
        await limits.CheckAsync("login", email, ip);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        // Identity updates the failure counter with optimistic concurrency. Serialize attempts for this account.
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE normalized_email = {email} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null || user.DeletedAt.HasValue || await users.IsLockedOutAsync(user))
        {
            throw new AccountException(AccountError.InvalidSession);
        }

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            EnsureUpdated(await users.AccessFailedAsync(user));
            await tx.CommitAsync(cancellationToken);
            throw new AccountException(AccountError.InvalidSession);
        }

        if (user.Status != AccountStatus.ACTIVE || !user.EmailConfirmed)
        {
            throw new AccountException(AccountError.InactiveAccount);
        }

        EnsureUpdated(await users.ResetAccessFailedCountAsync(user));
        var response = tokens.Issue(db.AuthSessions.Add(new AuthSession { UserId = user.Id }).Entity);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return response;
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = SessionTokens.Hash(refreshToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        // Lock the user before the session so a status change and its revocations serialize with renewal.
        var user = await db.Users.FromSqlInterpolated($"""
            SELECT u.* FROM users u JOIN auth_sessions s ON s.user_id = u.id
            WHERE s.refresh_token_hash = {hash} FOR UPDATE OF u
            """).SingleOrDefaultAsync(cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        if (user is null || user.Status != AccountStatus.ACTIVE || user.DeletedAt.HasValue || !user.EmailConfirmed)
        {
            throw new AccountException(AccountError.InvalidSession);
        }

        var session = await db.AuthSessions.SingleOrDefaultAsync(
            s => s.UserId == user.Id && s.RefreshTokenHash == hash && s.RevokedAt == null && s.DeletedAt == null && s.ExpiresAt > now,
            cancellationToken) ?? throw new AccountException(AccountError.InvalidSession);
        var response = tokens.Issue(session);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AccountException(AccountError.InvalidSession);
        }

        await tx.CommitAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(Guid userId, Guid sessionId, string refreshToken, CancellationToken cancellationToken)
    {
        var hash = SessionTokens.Hash(refreshToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var changed = await db.AuthSessions
            .Where(s => s.UserId == userId && s.Id == sessionId && s.RefreshTokenHash == hash
                && s.RevokedAt == null && s.ExpiresAt > now && s.DeletedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(s => s.RevokedAt, now), cancellationToken);
        if (changed == 0)
        {
            throw new AccountException(AccountError.InvalidSession);
        }
    }

    private static void EnsureUpdated(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new AccountException(AccountError.RegistrationConflict);
        }
    }
}
