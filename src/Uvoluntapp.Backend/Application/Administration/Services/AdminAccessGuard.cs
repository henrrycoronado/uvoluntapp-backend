using Uvoluntapp.Backend.Application.Administration.Ports;
using Uvoluntapp.Backend.Application.Authorization;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Services;

/// <summary>Exige que el actor sea ADMIN global activo. Es el único punto de autorización de administración.</summary>
public sealed class AdminAccessGuard(
    IAccountDirectory accounts,
    IRoleAssignmentStore roles,
    ContextAuthorizationService authorization,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<RoleAssignment>> RequireGlobalAdminAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var status = await accounts.FindStatusAsync(actorId, cancellationToken);
        var assignments = await roles.ListForUserAsync(actorId, cancellationToken);
        var decision = authorization.Authorize(new AuthorizationRequest(
            IsAuthenticated: true,
            AccountStatus: status ?? AccountStatus.PRE_REGISTERED,
            Assignments: assignments,
            RequiredRole: AssignedRole.ADMIN,
            ResourceScope: RoleScope.GLOBAL,
            ResourceId: null,
            ResourceProgramId: null,
            ResourceStateAllowsOperation: true,
            AtUtc: clock.GetUtcNow().UtcDateTime));

        return decision == AuthorizationDecision.ALLOWED
            ? assignments
            : throw new ForbiddenException("Se requiere un ADMIN global activo.");
    }
}
