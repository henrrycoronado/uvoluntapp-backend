namespace Uvoluntapp.Backend.Application.Accounts.Email;

public sealed class AccountEmailException(AccountEmailError error, string message)
    : Exception(message)
{
    public AccountEmailError Error { get; } = error;
}
