using System.Text.Json.Nodes;
using Uvoluntapp.Backend.Application.DTOs.Audit;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Interfaces.Audit;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Audit;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Audit;

public sealed class AuditQueryService(IAuditEventStore store)
{
    public async Task<PaginatedAuditEvents> ListAsync(
        AuditEventFilter filter,
        Guid actorId,
        IEnumerable<RoleAssignment> actorRoles,
        DateTime? now = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(actorRoles);

        EnsureAdmin(actorId, actorRoles, now ?? DateTime.UtcNow);

        var normalized = filter.Normalize();
        var result = await store.QueryAsync(normalized, cancellationToken);
        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)normalized.PageSize);

        return new PaginatedAuditEvents(
            result.Items.Select(ToResponse).ToList(),
            new PaginationMeta(normalized.Page, normalized.PageSize, result.TotalCount, totalPages));
    }

    private static void EnsureAdmin(Guid actorId, IEnumerable<RoleAssignment> actorRoles, DateTime now)
    {
        var isAdmin = actorId != Guid.Empty
            && actorRoles.Any(role =>
                role.UserId == actorId
                && role.Role == AssignedRole.ADMIN
                && role.Scope == RoleScope.GLOBAL
                && role.RevokedAt is null
                && (role.ValidFrom is null || role.ValidFrom <= now)
                && (role.ValidUntil is null || role.ValidUntil >= now));

        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Solo un ADMIN activo puede consultar la auditoría.");
        }
    }

    private static AuditEventResponse ToResponse(AuditEvent auditEvent) =>
        new(
            auditEvent.Id,
            auditEvent.ActorId,
            auditEvent.ActorId is null ? AuditActorType.SYSTEM : AuditActorType.USER,
            auditEvent.Action,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            ParseDetails(auditEvent.Data),
            auditEvent.CreatedAt);

    private static JsonNode? ParseDetails(string data) =>
        string.IsNullOrWhiteSpace(data) ? null : JsonNode.Parse(data);
}
