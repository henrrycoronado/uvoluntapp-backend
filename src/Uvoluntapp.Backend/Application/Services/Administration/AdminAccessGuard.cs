using Uvoluntapp.Backend.Application.DTOs.Authorization;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Services.Authorization;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Administration;

/// <summary>Resuelve los permisos del actor. Es el único punto de autorización de administración.</summary>
public sealed class AdminAccessGuard(
    IAccountDirectory accounts,
    IRoleAssignmentStore roles,
    ContextAuthorizationService authorization,
    TimeProvider clock)
{
    public async Task<ActorAccess> ResolveAsync(Guid actorId, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
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
            AtUtc: now));

        return new ActorAccess(
            actorId,
            status == AccountStatus.ACTIVE,
            decision == AuthorizationDecision.ALLOWED,
            assignments,
            now);
    }

    public async Task<IReadOnlyList<RoleAssignment>> RequireGlobalAdminAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAsync(actorId, cancellationToken);
        return access.IsGlobalAdmin
            ? access.Assignments
            : throw new ForbiddenException("Se requiere un ADMIN global activo.");
    }
}
