namespace Uvoluntapp.Backend.Application.Exceptions.Accounts;

public enum AccountSuspensionError
{
    // 403: el actor no es ADMIN global activo.
    Forbidden,

    // 404: la cuenta no existe o no es visible.
    AccountNotFound,

    // 409: el estado actual no permite la transición solicitada.
    InvalidTransition,

    // 400: falta el motivo de suspensión.
    InvalidReason,
}
