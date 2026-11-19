namespace Uvoluntapp.Backend.Application.Exceptions.Accounts;

public sealed class AccountException(AccountError error) : Exception
{
    public AccountError Error { get; } = error;
}
