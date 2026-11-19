namespace Uvoluntapp.Backend.Application.Exceptions.Accounts;

public sealed class AccountSuspensionException(AccountSuspensionError error, string message)
    : Exception(message)
{
    public AccountSuspensionError Error { get; } = error;
}
