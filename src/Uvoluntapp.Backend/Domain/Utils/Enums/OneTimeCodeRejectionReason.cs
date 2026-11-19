namespace Uvoluntapp.Backend.Domain.Utils.Enums;

public enum OneTimeCodeRejectionReason
{
    INVALID_CODE,
    EXPIRED,
    ATTEMPTS_EXHAUSTED,
    ALREADY_USED
}
