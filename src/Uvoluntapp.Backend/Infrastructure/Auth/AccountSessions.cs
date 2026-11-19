using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Auth;

public sealed class AccountSessions(
    AppDbContext db,
    UserManager<AppUser> users,
    AccountRateLimiter limits,
    SessionTokens tokens,
    RegistrationCodes codes,
    IEmailSender mail,
    TimeProvider clock,
    ILogger<AccountSessions> logger) : IAccountSessions
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

        // Una cuenta creada con Google no tiene contraseña: se prueba el buzón una vez para crearla.
        if (user.PasswordHash is null)
        {
            await StartPasswordSetupAsync(user, email, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            throw new AccountException(AccountError.PasswordSetupRequired);
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

    public async Task<SetPasswordResponse> SetPasswordAsync(SetPasswordRequest request, string ip, CancellationToken cancellationToken)
    {
        var email = users.NormalizeEmail(request.Email.Trim())!;
        await limits.CheckAsync("login", email, ip);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE normalized_email = {email} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        // Sin cuenta elegible no hay sello: el intento gasta el mismo presupuesto y falla igual.
        var eligible = user is { PasswordHash: null, DeletedAt: null, EmailConfirmed: true };
        await codes.VerifyAsync(email, eligible ? user!.SecurityStamp : null, request.Code);

        // AddPassword cambia el sello de seguridad, así que el código no se puede reutilizar.
        var added = await users.AddPasswordAsync(user!, request.Password);
        if (!added.Succeeded)
        {
            throw new AccountException(AccountError.InvalidInput);
        }

        TokenResponse? session = null;
        if (user!.Status == AccountStatus.ACTIVE)
        {
            session = tokens.Issue(db.AuthSessions.Add(new AuthSession { UserId = user.Id }).Entity);
            await db.SaveChangesAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return new SetPasswordResponse { AccountStatus = user.Status.ToString(), Tokens = session };
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

    private async Task StartPasswordSetupAsync(AppUser user, string normalizedEmail, CancellationToken cancellationToken)
    {
        // Solo cuentas con buzón verificado y no bloqueadas por estado reciben el código.
        var eligible = user.EmailConfirmed && user.Status is AccountStatus.ACTIVE or AccountStatus.PENDING_APPROVAL;
        if (!eligible)
        {
            throw new AccountException(AccountError.InvalidSession);
        }

        var code = await codes.IssueAsync(normalizedEmail, user.SecurityStamp);
        try
        {
            await mail.SendAsync(
                user.Email!,
                "Crea tu contraseña en Uvoluntapp",
                $"Tu código para crear tu contraseña es {code}. Vence en 10 minutos. Si no fuiste tú, ignora este correo.",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Un nuevo intento de inicio de sesión emite otro código si el proveedor falló.
            logger.LogError("El proveedor de correo no pudo entregar el código de contraseña ({ErrorType}).", ex.GetType().Name);
        }
    }
}
