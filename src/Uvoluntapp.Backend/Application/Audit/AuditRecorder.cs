using Uvoluntapp.Backend.Domain;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Domain.Audit;

namespace Uvoluntapp.Backend.Application.Audit;

public sealed class AuditRecorder(IAuditEventStore store)
{
    public Task RecordAsync(
        Guid actorId,
        string action,
        string resourceType,
        Guid resourceId,
        IReadOnlyDictionary<string, object?>? details = null,
        DateTime? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = AuditEvent.Create(
            actorId,
            action,
            resourceType,
            resourceId,
            details,
            occurredAt);

        return store.AddAsync(auditEvent, cancellationToken);
    }

    public Task RecordSystemAsync(
        string action,
        string resourceType,
        Guid resourceId,
        IReadOnlyDictionary<string, object?>? details = null,
        DateTime? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = AuditEvent.CreateSystem(
            action,
            resourceType,
            resourceId,
            details,
            occurredAt);

        return store.AddAsync(auditEvent, cancellationToken);
    }

    public Task RecordRoleAssignedAsync(
        Guid actorId,
        RoleAssignment assignment,
        DateTime? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return RecordAsync(
            actorId,
            AuditActions.RoleAssigned,
            AuditResourceTypes.RoleAssignment,
            assignment.Id,
            new Dictionary<string, object?>
            {
                ["userId"] = assignment.UserId,
                ["role"] = assignment.Role.ToString(),
                ["scope"] = assignment.Scope.ToString(),
                ["scopeId"] = assignment.ScopeId,
            },
            occurredAt,
            cancellationToken);
    }

    public Task RecordRoleRevokedAsync(
        Guid actorId,
        RoleAssignment assignment,
        DateTime? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return RecordAsync(
            actorId,
            AuditActions.RoleRevoked,
            AuditResourceTypes.RoleAssignment,
            assignment.Id,
            new Dictionary<string, object?>
            {
                ["userId"] = assignment.UserId,
                ["role"] = assignment.Role.ToString(),
                ["revokedAt"] = assignment.RevokedAt,
            },
            occurredAt,
            cancellationToken);
    }

    public Task RecordAccountApprovedAsync(
        Guid actorId,
        AccountApprovalRequest request,
        DateTime? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return RecordAsync(
            actorId,
            AuditActions.AccountApproved,
            AuditResourceTypes.AccountApprovalRequest,
            request.Id,
            new Dictionary<string, object?>
            {
                ["userId"] = request.UserId,
                ["status"] = AccountApprovalStatus.ACTIVE.ToString(),
            },
            occurredAt,
            cancellationToken);
    }

    public Task RecordAccountRejectedAsync(
        Guid actorId,
        AccountApprovalRequest request,
        string reason,
        DateTime? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("El motivo de rechazo es obligatorio.", nameof(reason));
        }

        return RecordAsync(
            actorId,
            AuditActions.AccountRejected,
            AuditResourceTypes.AccountApprovalRequest,
            request.Id,
            new Dictionary<string, object?>
            {
                ["userId"] = request.UserId,
                ["status"] = AccountApprovalStatus.REJECTED.ToString(),
                ["reason"] = reason.Trim(),
            },
            occurredAt,
            cancellationToken);
    }

    public Task RecordAccountStatusChangedAsync(
        Guid actorId,
        Guid userId,
        AccountStatus previous,
        AccountStatus next,
        string? reason = null,
        DateTime? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        if (previous == next)
        {
            throw new ArgumentException("El estado nuevo debe ser distinto del anterior.", nameof(next));
        }

        return RecordAsync(
            actorId,
            AuditActions.AccountStatusChanged,
            AuditResourceTypes.User,
            userId,
            new Dictionary<string, object?>
            {
                ["previousStatus"] = previous.ToString(),
                ["newStatus"] = next.ToString(),
                ["reason"] = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            },
            occurredAt,
            cancellationToken);
    }
}
