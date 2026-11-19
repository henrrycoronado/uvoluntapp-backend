namespace Uvoluntapp.Backend.Domain.Accounts;

// Únicas transiciones admitidas por SCRUM-61: ACTIVE -> SUSPENDED -> ACTIVE.
public static class AccountStatusTransitions
{
    public static bool CanSuspend(AccountStatus current) => current == AccountStatus.ACTIVE;

    public static bool CanReactivate(AccountStatus current) => current == AccountStatus.SUSPENDED;

    public static AccountStatus Suspend(AccountStatus current)
    {
        if (!CanSuspend(current))
        {
            throw new InvalidAccountStatusTransitionException(current, AccountStatus.SUSPENDED);
        }

        return AccountStatus.SUSPENDED;
    }

    public static AccountStatus Reactivate(AccountStatus current)
    {
        if (!CanReactivate(current))
        {
            throw new InvalidAccountStatusTransitionException(current, AccountStatus.ACTIVE);
        }

        return AccountStatus.ACTIVE;
    }
}
