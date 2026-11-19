namespace Uvoluntapp.Backend.Domain.Utils.Enums;

// Los nombres son los valores persistidos y publicados en OpenAPI.
public enum AccountStatus
{
    PRE_REGISTERED,
    PENDING_VERIFICATION,
    PENDING_APPROVAL,
    ACTIVE,
    REJECTED,
    SUSPENDED,
}
