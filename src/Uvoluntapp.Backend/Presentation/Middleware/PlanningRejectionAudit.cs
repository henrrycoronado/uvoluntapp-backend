using Microsoft.IdentityModel.JsonWebTokens;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Presentation.Middleware;

/// <summary>
/// Audita las operaciones de M2 rechazadas (SCRUM-50, criterio 6): permiso denegado (403), estado incompatible
/// o concurrencia (409) y regla de negocio incumplida (422) sobre programas y actividades.
/// Usa un ámbito propio para no guardar cambios a medias de la petición que falló.
/// </summary>
internal sealed class PlanningRejectionAudit(IServiceScopeFactory scopes, ILogger<PlanningRejectionAudit> logger)
{
    public async Task RecordAsync(HttpContext context, int status, string reason, CancellationToken cancellationToken)
    {
        if (status is not (403 or 409 or 422) || HttpMethods.IsGet(context.Request.Method))
        {
            return;
        }

        // /api/v1/{programs|activities}/{id?}/{operación?}
        var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (segments.Length < 3 || segments[2] is not ("programs" or "activities"))
        {
            return;
        }

        if (!Guid.TryParse(context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var actorId))
        {
            return;
        }

        var resourceId = segments.Length > 3 && Guid.TryParse(segments[3], out var id) ? id : Guid.Empty;
        var operation = string.Join('/', segments.Skip(resourceId == Guid.Empty ? 3 : 4));
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var audit = scope.ServiceProvider.GetRequiredService<AuditRecorder>();
            await audit.RecordAsync(
                actorId,
                AuditActions.PlanningOperationRejected,
                segments[2] == "programs" ? AuditResourceTypes.Program : AuditResourceTypes.Activity,
                resourceId,
                new Dictionary<string, object?>
                {
                    ["method"] = context.Request.Method,
                    ["operation"] = operation.Length == 0 ? null : operation,
                    ["status"] = status,
                    ["reason"] = reason,
                },
                cancellationToken: cancellationToken);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // La auditoría del rechazo nunca cambia la respuesta al cliente.
            logger.LogWarning(ex, "No se pudo auditar el rechazo de {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
    }
}
