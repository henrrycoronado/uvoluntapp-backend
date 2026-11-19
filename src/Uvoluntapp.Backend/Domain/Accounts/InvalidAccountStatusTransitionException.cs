namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed class InvalidAccountStatusTransitionException(AccountStatus from, AccountStatus to)
    : InvalidOperationException($"Transición de cuenta no permitida: {from} -> {to}.")
{
    public AccountStatus From { get; } = from;

    public AccountStatus To { get; } = to;
}
