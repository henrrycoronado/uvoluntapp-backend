namespace Uvoluntapp.Backend.Domain.Accounts;

public enum OneTimeCodeRejectionReason
{
    INVALID_CODE,
    EXPIRED,
    ATTEMPTS_EXHAUSTED,
    ALREADY_USED
}
