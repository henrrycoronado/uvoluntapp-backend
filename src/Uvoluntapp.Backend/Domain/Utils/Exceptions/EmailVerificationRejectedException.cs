using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Utils.Exceptions;

public sealed class EmailVerificationRejectedException(OneTimeCodeRejectionReason reason)
    : Exception("El código de verificación no es válido.")
{
    public OneTimeCodeRejectionReason Reason { get; } = reason;
}
