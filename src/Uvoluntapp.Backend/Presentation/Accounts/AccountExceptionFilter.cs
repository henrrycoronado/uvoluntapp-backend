using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Uvoluntapp.Backend.Application.Accounts;

namespace Uvoluntapp.Backend.Presentation.Accounts;

public sealed class AccountExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AccountException error)
        {
            return;
        }

        var (status, title) = error.Error switch
        {
            AccountError.InvalidInput => (400, "Datos de registro inválidos."),
            AccountError.InvalidSession => (401, "Credenciales o sesión inválidas."),
            AccountError.RegistrationConflict => (409, "No se puede completar la operación de registro."),
            AccountError.InactiveAccount => (422, "La cuenta no está activa."),
            AccountError.InvalidCode => (422, "Código inválido o vencido."),
            AccountError.TooManyAttempts => (429, "Límite de intentos excedido."),
            AccountError.PasswordSetupRequired => (409, "La cuenta aún no tiene contraseña. Enviamos un código a tu correo para crearla."),
            _ => throw new ArgumentOutOfRangeException(nameof(context))
        };
        context.Result = new ObjectResult(new ProblemDetails { Type = TypeOf(error.Error), Title = title, Status = status })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }

    // La web distingue este 409 de un conflicto de registro por el tipo, no por el texto.
    private static string TypeOf(AccountError error) =>
        error == AccountError.PasswordSetupRequired ? "urn:uvoluntapp:password-setup-required" : "about:blank";
}
