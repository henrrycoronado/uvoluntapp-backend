using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Audit;
using Uvoluntapp.Backend.Domain;
using Uvoluntapp.Backend.Domain.Audit;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Administration;

public sealed class AuditEventStore(AppDbContext db) : IAuditEventStore
{
    public async Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) =>
        await db.AuditEvents.AddAsync(auditEvent, cancellationToken);

    public async Task<AuditEventQueryResult> QueryAsync(AuditEventFilter filter, CancellationToken cancellationToken = default)
    {
        var normalized = filter.Normalize();
        var query = db.AuditEvents.AsQueryable();

        if (normalized.ActorId is { } actorId)
        {
            query = query.Where(e => e.ActorId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(normalized.Action))
        {
            var action = normalized.Action.Trim();
            query = query.Where(e => e.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(normalized.EntityType))
        {
            var entityType = normalized.EntityType.Trim();
            query = query.Where(e => e.ResourceType == entityType);
        }

        if (normalized.EntityId is { } entityId)
        {
            query = query.Where(e => e.ResourceId == entityId);
        }

        if (normalized.From is { } from)
        {
            query = query.Where(e => e.CreatedAt >= from);
        }

        if (normalized.To is { } to)
        {
            query = query.Where(e => e.CreatedAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(cancellationToken);

        return new AuditEventQueryResult(items, total);
    }
}
