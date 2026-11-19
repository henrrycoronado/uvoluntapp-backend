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
            _ => throw new ArgumentOutOfRangeException(nameof(context))
        };
        context.Result = new ObjectResult(new ProblemDetails { Type = "about:blank", Title = title, Status = status })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }
}
