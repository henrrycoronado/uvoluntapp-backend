using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Application.Services.Accounts;

// SCRUM-109: cambio de correo con OTP en el correo nuevo y correo institucional verificado.
public sealed class AccountEmailService(IAccountEmailStore store)
{
    public async Task<EmailVerificationRequestResult> RequestEmailChangeAsync(
        Guid userId,
        string newEmail,
        string currentPassword,
        string code,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentPassword);

        var requestedAt = now ?? DateTimeOffset.UtcNow;
        if (!EmailAddress.TryCreate(newEmail, out var target))
        {
            throw InvalidEmail();
        }

        var account = await FindAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(account.Email)
            && string.Equals(target.Value, account.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new AccountEmailException(
                AccountEmailError.EmailUnchanged,
                "El nuevo correo debe ser distinto del correo actual.");
        }

        if (!await store.VerifyPasswordAsync(userId, currentPassword, cancellationToken))
        {
            throw new AccountEmailException(
                AccountEmailError.InvalidPassword,
                "La contraseña actual no es correcta.");
        }

        if (await store.IsEmailInUseAsync(target.Value, userId, cancellationToken))
        {
            throw EmailInUse();
        }

        var pending = account.RequestEmailChange(target.Value, code, requestedAt);
        await store.SavePendingAsync(userId, pending, cancellationToken);
        return new EmailVerificationRequestResult(userId, pending.Email, pending.Purpose, pending.ExpiresAt);
    }

    public async Task<EmailChangeResult> ConfirmEmailChangeAsync(
        Guid userId,
        string code,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var confirmedAt = now ?? DateTimeOffset.UtcNow;
        var account = await FindAsync(userId, cancellationToken);
        var pending = await FindPendingAsync(userId, EmailVerificationPurpose.EMAIL_CHANGE, cancellationToken);
        if (await store.IsEmailInUseAsync(pending.Email, userId, cancellationToken))
        {
            throw EmailInUse();
        }

        try
        {
            account.ConfirmEmailChange(pending, code, confirmedAt);
        }
        catch (EmailVerificationRejectedException exception)
        {
            // Conserva el intento fallido para que el límite de 5 intentos sobreviva entre solicitudes.
            await store.SavePendingAsync(userId, pending, cancellationToken);
            throw VerificationFailure(exception.Reason);
        }

        var sessions = await store.ListSessionsAsync(userId, cancellationToken);
        var sessionIdsToRevoke = RevokeActiveSessions(userId, sessions, confirmedAt);
        await store.SaveAsync(account, cancellationToken);
        await store.RemovePendingAsync(userId, EmailVerificationPurpose.EMAIL_CHANGE, cancellationToken);

        return new EmailChangeResult(
            userId,
            account.Email,
            account.EmailVerifiedAt ?? confirmedAt.UtcDateTime,
            sessionIdsToRevoke);
    }

    public async Task<EmailVerificationRequestResult> RequestInstitutionalEmailAsync(
        Guid userId,
        string institutionalEmail,
        string code,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var requestedAt = now ?? DateTimeOffset.UtcNow;
        if (!EmailAddress.TryCreate(institutionalEmail, out var target))
        {
            throw InvalidEmail();
        }

        if (!target.IsInstitutional)
        {
            throw new AccountEmailException(
                AccountEmailError.InstitutionalDomainRequired,
                "El correo institucional debe terminar en @ucb.edu.bo.");
        }

        var account = await FindAsync(userId, cancellationToken);
        if (await store.IsEmailInUseAsync(target.Value, userId, cancellationToken))
        {
            throw EmailInUse();
        }

        var pending = account.RequestInstitutionalEmail(target.Value, code, requestedAt);
        await store.SavePendingAsync(userId, pending, cancellationToken);
        return new EmailVerificationRequestResult(userId, pending.Email, pending.Purpose, pending.ExpiresAt);
    }

    public async Task<InstitutionalEmailResult> ConfirmInstitutionalEmailAsync(
        Guid userId,
        string code,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var confirmedAt = now ?? DateTimeOffset.UtcNow;
        var account = await FindAsync(userId, cancellationToken);
        var pending = await FindPendingAsync(
            userId,
            EmailVerificationPurpose.INSTITUTIONAL_EMAIL,
            cancellationToken);
        if (await store.IsEmailInUseAsync(pending.Email, userId, cancellationToken))
        {
            throw EmailInUse();
        }

        try
        {
            account.ConfirmInstitutionalEmail(pending, code, confirmedAt);
        }
        catch (EmailVerificationRejectedException exception)
        {
            await store.SavePendingAsync(userId, pending, cancellationToken);
            throw VerificationFailure(exception.Reason);
        }

        await store.SaveAsync(account, cancellationToken);
        await store.RemovePendingAsync(
            userId,
            EmailVerificationPurpose.INSTITUTIONAL_EMAIL,
            cancellationToken);

        return new InstitutionalEmailResult(
            userId,
            account.InstitutionalEmail!,
            account.InstitutionalEmailVerifiedAt ?? confirmedAt.UtcDateTime);
    }

    private static IReadOnlyList<Guid> RevokeActiveSessions(
        Guid userId,
        IEnumerable<AuthSession> sessions,
        DateTimeOffset now)
    {
        var revokedSessionIds = new List<Guid>();

        foreach (var session in sessions)
        {
            if (session.UserId != userId || !session.IsActive(now.UtcDateTime))
            {
                continue;
            }

            session.Revoke(now.UtcDateTime);
            revokedSessionIds.Add(session.Id);
        }

        return revokedSessionIds;
    }

    private static AccountEmailException InvalidEmail() =>
        new(AccountEmailError.InvalidEmail, "El correo no tiene un formato válido.");

    private static AccountEmailException EmailInUse() =>
        new(AccountEmailError.EmailAlreadyInUse, "El correo ya pertenece a otra cuenta.");

    private static AccountEmailException VerificationFailure(OneTimeCodeRejectionReason reason) =>
        reason switch
        {
            OneTimeCodeRejectionReason.EXPIRED => new AccountEmailException(
                AccountEmailError.VerificationExpired,
                "El código de verificación expiró."),
            OneTimeCodeRejectionReason.ATTEMPTS_EXHAUSTED => new AccountEmailException(
                AccountEmailError.TooManyAttempts,
                "Se agotaron los intentos del código de verificación."),
            _ => new AccountEmailException(
                AccountEmailError.InvalidCode,
                "El código de verificación no es válido."),
        };

    private async Task<AccountEmail> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        await store.FindAsync(userId, cancellationToken)
        ?? throw new AccountEmailException(AccountEmailError.AccountNotFound, "La cuenta no existe.");

    private async Task<PendingEmailVerification> FindPendingAsync(
        Guid userId,
        EmailVerificationPurpose purpose,
        CancellationToken cancellationToken) =>
        await store.FindPendingAsync(userId, purpose, cancellationToken)
        ?? throw new AccountEmailException(
            AccountEmailError.VerificationNotRequested,
            "No hay una verificación pendiente para esa operación.");
}
