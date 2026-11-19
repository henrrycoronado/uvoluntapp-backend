using Uvoluntapp.Backend.Domain;

namespace Uvoluntapp.Backend.Application.Audit;

public interface IAuditEventStore
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    // Implementación esperada: aplicar todos los filtros, contar el total y ordenar por created_at DESC.
    Task<AuditEventQueryResult> QueryAsync(
        AuditEventFilter filter,
        CancellationToken cancellationToken = default);
}
