namespace Uvoluntapp.Backend.Application.Exceptions.Accounts;

public sealed class AccountEmailException(AccountEmailError error, string message)
    : Exception(message)
{
    public AccountEmailError Error { get; } = error;
}
