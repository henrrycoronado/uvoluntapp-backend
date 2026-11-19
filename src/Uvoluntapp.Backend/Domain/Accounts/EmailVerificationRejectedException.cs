namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed class EmailVerificationRejectedException(OneTimeCodeRejectionReason reason)
    : Exception("El código de verificación no es válido.")
{
    public OneTimeCodeRejectionReason Reason { get; } = reason;
}
