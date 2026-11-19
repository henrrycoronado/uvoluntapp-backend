namespace Uvoluntapp.Backend.Application.Accounts.Suspension;

public sealed class AccountSuspensionException(AccountSuspensionError error, string message)
    : Exception(message)
{
    public AccountSuspensionError Error { get; } = error;
}
