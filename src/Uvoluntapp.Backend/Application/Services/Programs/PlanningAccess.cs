using Uvoluntapp.Backend.Application.DTOs.Authorization;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Application.Services.Authorization;
using Uvoluntapp.Backend.Domain.Entities.Activities;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Programs;

/// <summary>
/// Único punto de autorización de M2 (programas y actividades). Reglas del usuario (19/11/2026):
/// - ADMIN gestiona todo; es el único que crea programas, crea actividades libres y cambia el programa de una actividad.
/// - PROGRAM_LEADER ve todos los programas y gestiona solo los asignados por un ADMIN, sus actividades
///   y las actividades libres que un ADMIN le asigne.
/// - OPERATIONAL_LEADER ve todos los programas y no gestiona; ve el detalle interno de su programa
///   y de las actividades a las que fue delegado.
/// - Las asignaciones vencidas o revocadas dejan de contar al instante.
/// </summary>
public sealed class PlanningAccess(AdminAccessGuard guard, ContextAuthorizationService authorization)
{
    public static bool CanSeeAllPrograms(ActorAccess actor) => actor.IsGlobalAdmin || actor.IsLeader;

    // Programas donde el actor ve borradores y datos internos (lidera u opera en ellos).
    public static IReadOnlyCollection<Guid> InternalProgramIds(ActorAccess actor) => actor.Assignments
        .Where(a => a.Scope == RoleScope.PROGRAM
            && a.ScopeId is not null
            && a.Role is AssignedRole.PROGRAM_LEADER or AssignedRole.OPERATIONAL_LEADER
            && a.IsActiveAt(actor.AtUtc))
        .Select(a => a.ScopeId!.Value)
        .Distinct()
        .ToList();

    public static IReadOnlyCollection<Guid> InternalActivityIds(ActorAccess actor) => actor.Assignments
        .Where(a => a.Scope == RoleScope.ACTIVITY
            && a.ScopeId is not null
            && a.Role is AssignedRole.PROGRAM_LEADER or AssignedRole.OPERATIONAL_LEADER
            && a.IsActiveAt(actor.AtUtc))
        .Select(a => a.ScopeId!.Value)
        .Distinct()
        .ToList();

    public async Task<ActorAccess> ResolveAsync(Guid actorId, CancellationToken cancellationToken = default)
    {
        var actor = await guard.ResolveAsync(actorId, cancellationToken);
        return actor.IsActive ? actor : throw new ForbiddenException("Tu cuenta no está activa.");
    }

    public bool CanManageProgram(ActorAccess actor, Guid programId) =>
        actor.IsGlobalAdmin || Allowed(actor, AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, programId, null);

    public bool CanManageActivity(ActorAccess actor, Activity activity) =>
        actor.IsGlobalAdmin || Allowed(actor, AssignedRole.PROGRAM_LEADER, RoleScope.ACTIVITY, activity.Id, activity.ProgramId);

    // Ver borradores y datos internos: quien gestiona y los líderes operativos de ese programa o delegados a la actividad.
    public bool CanViewActivityInternals(ActorAccess actor, Activity activity) =>
        CanManageActivity(actor, activity)
        || Allowed(actor, AssignedRole.OPERATIONAL_LEADER, RoleScope.ACTIVITY, activity.Id, activity.ProgramId);

    public void RequireAdmin(ActorAccess actor, string message)
    {
        if (!actor.IsGlobalAdmin)
        {
            throw new ForbiddenException(message);
        }
    }

    public void RequireProgramManager(ActorAccess actor, Guid programId)
    {
        if (!CanManageProgram(actor, programId))
        {
            throw new ForbiddenException("Solo un ADMIN o un líder asignado a este programa puede gestionarlo.");
        }
    }

    public void RequireActivityManager(ActorAccess actor, Activity activity)
    {
        if (!CanManageActivity(actor, activity))
        {
            throw new ForbiddenException("Solo un ADMIN o un líder responsable de esta actividad puede gestionarla.");
        }
    }

    private bool Allowed(ActorAccess actor, AssignedRole role, RoleScope scope, Guid resourceId, Guid? resourceProgramId) =>
        authorization.Authorize(new AuthorizationRequest(
            IsAuthenticated: true,
            AccountStatus: actor.IsActive ? AccountStatus.ACTIVE : AccountStatus.SUSPENDED,
            Assignments: actor.Assignments.ToList(),
            RequiredRole: role,
            ResourceScope: scope,
            ResourceId: resourceId,
            ResourceProgramId: resourceProgramId,
            ResourceStateAllowsOperation: true,
            AtUtc: actor.AtUtc)) == AuthorizationDecision.ALLOWED;
}
