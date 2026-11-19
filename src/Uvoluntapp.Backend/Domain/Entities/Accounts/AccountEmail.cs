using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

// Estado de los correos de una cuenta: el de acceso y el institucional verificado (SCRUM-109).
public sealed class AccountEmail
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime? EmailVerifiedAt { get; set; }

    public string? InstitutionalEmail { get; set; }

    public DateTime? InstitutionalEmailVerifiedAt { get; set; }

    public bool HasVerifiedInstitutionalEmail =>
        InstitutionalEmail is not null && InstitutionalEmailVerifiedAt is not null;

    // Solo el correo institucional verificado habilitará compromisos de horas (módulo futuro).
    public bool CanHaveHourCommitments => HasVerifiedInstitutionalEmail;

    public PendingEmailVerification RequestEmailChange(string newEmail, string code, DateTimeOffset now)
    {
        var target = EmailAddress.Create(newEmail);
        if (!string.IsNullOrWhiteSpace(Email)
            && string.Equals(target.Value, Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleViolationException("El nuevo correo debe ser distinto del correo actual.");
        }

        return PendingEmailVerification.Create(
            target.Value,
            EmailVerificationPurpose.EMAIL_CHANGE,
            code,
            now);
    }

    public void ConfirmEmailChange(PendingEmailVerification pending, string code, DateTimeOffset now)
    {
        EnsurePurpose(pending, EmailVerificationPurpose.EMAIL_CHANGE);
        EnsureVerification(pending, code, now);

        Email = pending.Email;
        EmailVerifiedAt = now.UtcDateTime;
    }

    public PendingEmailVerification RequestInstitutionalEmail(
        string institutionalEmail,
        string code,
        DateTimeOffset now)
    {
        var target = EmailAddress.Create(institutionalEmail);
        if (!target.IsInstitutional)
        {
            throw new DomainRuleViolationException("El correo institucional debe terminar en @ucb.edu.bo.");
        }

        return PendingEmailVerification.Create(
            target.Value,
            EmailVerificationPurpose.INSTITUTIONAL_EMAIL,
            code,
            now);
    }

    public void ConfirmInstitutionalEmail(PendingEmailVerification pending, string code, DateTimeOffset now)
    {
        EnsurePurpose(pending, EmailVerificationPurpose.INSTITUTIONAL_EMAIL);

        if (!InstitutionalEmailDomain.IsInstitutional(pending.Email))
        {
            throw new DomainRuleViolationException("El correo institucional debe terminar en @ucb.edu.bo.");
        }

        EnsureVerification(pending, code, now);
        InstitutionalEmail = pending.Email;
        InstitutionalEmailVerifiedAt = now.UtcDateTime;
    }

    private static void EnsurePurpose(
        PendingEmailVerification pending,
        EmailVerificationPurpose expected)
    {
        ArgumentNullException.ThrowIfNull(pending);
        if (pending.Purpose != expected)
        {
            throw new DomainRuleViolationException("La verificación pendiente no corresponde a esta operación.");
        }
    }

    private static void EnsureVerification(
        PendingEmailVerification pending,
        string code,
        DateTimeOffset now)
    {
        var result = pending.Verify(code, now);
        if (!result.IsValid)
        {
            throw new EmailVerificationRejectedException(result.RejectionReason!.Value);
        }
    }
}
