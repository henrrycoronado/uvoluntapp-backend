using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Activities;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Presentation.Middleware;

/// <summary>Traduce las excepciones de aplicación y dominio a Problem Details (AGENTS.md, regla 8).</summary>
internal sealed class GlobalExceptionHandler(PlanningRejectionAudit planningAudit) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var mapped = Map(exception);
        if (mapped is null)
        {
            return false;
        }

        var (status, title) = mapped.Value;
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails { Type = "about:blank", Title = title, Status = status, Detail = exception.Message };
        problem.Extensions["code"] = exception is EnrollmentException rejected ? rejected.Code : $"HTTP_{status}";
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        await planningAudit.RecordAsync(httpContext, status, exception.Message, cancellationToken);
        return true;
    }

    private static (int Status, string Title)? Map(Exception exception) => exception switch
    {
        EnrollmentException e => (e.Code == "INVALID_GROUP" ? 422 : 409, "Inscripción rechazada."),
        UnauthorizedAccessException => (401, "Sesión inválida."),
        NotFoundException => (404, "Recurso no encontrado."),
        ForbiddenException => (403, "Permiso denegado."),
        ConflictException => (409, "Conflicto con el estado actual."),
        DomainRuleViolationException => (422, "Regla de negocio incumplida."),
        AccountSuspensionException suspension => suspension.Error switch
        {
            AccountSuspensionError.Forbidden => (403, "Permiso denegado."),
            AccountSuspensionError.AccountNotFound => (404, "La cuenta no existe."),
            AccountSuspensionError.InvalidTransition => (409, "La transición no está permitida."),
            _ => (422, "El motivo es obligatorio."),
        },
        AccountEmailException email => email.Error switch
        {
            AccountEmailError.AccountNotFound => (404, "La cuenta no existe."),
            AccountEmailError.InvalidEmail => (400, "El correo no es válido."),
            AccountEmailError.EmailAlreadyInUse => (409, "El correo ya está en uso."),
            AccountEmailError.InvalidPassword => (401, "La contraseña no es correcta."),
            AccountEmailError.TooManyAttempts => (429, "Límite de intentos excedido."),
            _ => (422, "La operación no es válida en este estado."),
        },
        AccountException account => account.Error switch
        {
            AccountError.InvalidInput => (400, "Datos inválidos."),
            AccountError.InvalidSession => (401, "Credenciales o sesión inválidas."),
            AccountError.RegistrationConflict => (409, "No se puede completar la operación."),
            AccountError.InactiveAccount => (422, "La cuenta no está activa."),
            AccountError.InvalidCode => (422, "Código inválido o vencido."),
            AccountError.TooManyAttempts => (429, "Límite de intentos excedido."),
            _ => (400, "Datos inválidos."),
        },
        ArgumentException => (400, "Datos inválidos."),
        DbUpdateConcurrencyException => (409, "Otra persona modificó este recurso. Recarga y vuelve a intentar."),
        DbUpdateException db => Constraint(db),
        _ => null,
    };

    /// <summary>Traduce las violaciones de restricciones de PostgreSQL a estados HTTP de negocio.</summary>
    private static (int Status, string Title)? Constraint(DbUpdateException exception) =>
        exception.InnerException is PostgresException pg ? pg.SqlState switch
        {
            "23503" => (422, "Una referencia no existe."),
            "23505" => (409, "Ya existe un registro con esos datos."),
            "23514" => (422, "Los datos incumplen una regla del esquema."),
            _ => null,
        } : null;
}
