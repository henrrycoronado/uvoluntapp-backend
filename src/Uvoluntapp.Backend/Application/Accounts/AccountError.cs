namespace Uvoluntapp.Backend.Application.Accounts;

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
