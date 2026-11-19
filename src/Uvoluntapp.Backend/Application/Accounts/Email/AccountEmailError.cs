namespace Uvoluntapp.Backend.Application.Accounts.Email;

public enum AccountEmailError
{
    // 404: la cuenta no existe o no es visible.
    AccountNotFound,

    // 400: el correo no tiene un formato válido.
    InvalidEmail,

    // 422: el nuevo correo de acceso es igual al actual.
    EmailUnchanged,

    // 409: el correo ya pertenece a otra cuenta.
    EmailAlreadyInUse,

    // 401: la contraseña actual no confirma la operación.
    InvalidPassword,

    // 422: el código no coincide, ya fue usado o no existe un desafío pendiente del tipo esperado.
    InvalidCode,

    // 422: el código expiró.
    VerificationExpired,

    // 429: se agotaron los intentos del código.
    TooManyAttempts,

    // 409: no hay una verificación pendiente para esa operación.
    VerificationNotRequested,

    // 422: el correo institucional no pertenece a ucb.edu.bo.
    InstitutionalDomainRequired,
}
