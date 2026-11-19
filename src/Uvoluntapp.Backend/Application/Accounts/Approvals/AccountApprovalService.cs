using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Approvals;

public sealed class AccountApprovalService
{
    public void Approve(
        Guid actorId,
        AccountApprovalRequest request,
        IEnumerable<RoleAssignment> actorRoles,
        DateTime? now = null)
    {
        var reviewedAt = now ?? DateTime.UtcNow;
        EnsureAdmin(actorId, actorRoles, reviewedAt);
        request.Approve(actorId, reviewedAt);
    }

    public void Reject(
        Guid actorId,
        AccountApprovalRequest request,
        IEnumerable<RoleAssignment> actorRoles,
        string reason,
        DateTime? now = null)
    {
        var reviewedAt = now ?? DateTime.UtcNow;
        EnsureAdmin(actorId, actorRoles, reviewedAt);
        request.Reject(actorId, reason, reviewedAt);
    }

    public AccountApprovalRequest CreateRequest(
        Guid userId,
        IEnumerable<AccountApprovalRequest> history,
        string firstName,
        string lastName,
        string? phone,
        string relationship,
        DateTime? now = null)
    {
        var latest = history
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .FirstOrDefault();

        if (latest is not null && latest.Status != AccountApprovalStatus.REJECTED)
        {
            throw new InvalidOperationException("La cuenta ya tiene una solicitud abierta o aprobada.");
        }

        if (latest is not null && !latest.CanRequestAgainAt(now ?? DateTime.UtcNow))
        {
            throw new InvalidOperationException("Debe transcurrir un día desde el rechazo.");
        }

        return new AccountApprovalRequest
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Relationship = relationship,
        };
    }

    private static void EnsureAdmin(Guid actorId, IEnumerable<RoleAssignment> actorRoles, DateTime now)
    {
        if (!actorRoles.Any(role =>
                role.UserId == actorId
                && role.Role == AssignedRole.ADMIN
                && role.Scope == RoleScope.GLOBAL
                && role.IsActiveAt(now)))
        {
            throw new UnauthorizedAccessException("Solo un ADMIN activo puede resolver solicitudes.");
        }
    }
}
