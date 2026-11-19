namespace Uvoluntapp.Backend.Application.Exceptions.Accounts;

public enum AccountError
{
    InvalidInput,
    InvalidSession,
    InvalidCode,
    RegistrationConflict,
    InactiveAccount,
    TooManyAttempts,
    PasswordSetupRequired
}
