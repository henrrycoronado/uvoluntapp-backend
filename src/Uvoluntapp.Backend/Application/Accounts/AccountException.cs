namespace Uvoluntapp.Backend.Application.Accounts;

public sealed class AccountException(AccountError error) : Exception
{
    public AccountError Error { get; } = error;
}
