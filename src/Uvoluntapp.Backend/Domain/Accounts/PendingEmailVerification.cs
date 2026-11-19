namespace Uvoluntapp.Backend.Domain.Accounts;

// Desafío OTP pendiente para cambiar el correo de acceso o verificar el institucional.
public sealed class PendingEmailVerification
{
    private PendingEmailVerification(
        string email,
        EmailVerificationPurpose purpose,
        OneTimeCode code,
        DateTimeOffset requestedAt)
    {
        Email = email;
        Purpose = purpose;
        Code = code;
        RequestedAt = requestedAt;
    }

    public string Email { get; }

    public EmailVerificationPurpose Purpose { get; }

    public OneTimeCode Code { get; }

    public DateTimeOffset RequestedAt { get; }

    public DateTimeOffset ExpiresAt => Code.ExpiresAt;

    public static PendingEmailVerification Create(
        string email,
        EmailVerificationPurpose purpose,
        string code,
        DateTimeOffset requestedAt)
    {
        var address = EmailAddress.Create(email);
        return new PendingEmailVerification(
            address.Value,
            purpose,
            OneTimeCode.Create(code, requestedAt),
            requestedAt);
    }

    // Reconstruye el desafío al recuperarlo de un almacén de códigos.
    public static PendingEmailVerification Restore(
        string email,
        EmailVerificationPurpose purpose,
        OneTimeCode code,
        DateTimeOffset requestedAt)
    {
        ArgumentNullException.ThrowIfNull(code);

        var address = EmailAddress.Create(email);
        return new PendingEmailVerification(address.Value, purpose, code, requestedAt);
    }

    public OneTimeCodeVerificationResult Verify(string code, DateTimeOffset now) => Code.Verify(code, now);
}
