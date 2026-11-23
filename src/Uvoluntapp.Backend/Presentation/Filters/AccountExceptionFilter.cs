using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;

namespace Uvoluntapp.Backend.Presentation.Filters;

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
            AccountError.ReapplyTooEarly => (422, "Todavía no puedes enviar una nueva solicitud."),
            _ => throw new ArgumentOutOfRangeException(nameof(context))
        };
        var problem = new ProblemDetails { Type = TypeOf(error.Error), Title = title, Status = status };
        problem.Extensions["code"] = error.Error.ToString();
        foreach (var (key, value) in error.Extensions)
        {
            problem.Extensions[key] = value;
        }

        context.Result = new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }

    // El tipo identifica respuestas de estado para los clientes existentes.
    private static string TypeOf(AccountError error) => error switch
    {
        AccountError.InactiveAccount => "urn:uvoluntapp:inactive-account",
        AccountError.ReapplyTooEarly => "urn:uvoluntapp:reapply-too-early",
        _ => "about:blank",
    };
}
