using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Policies;

/// <summary>
/// Quién puede asignar o revocar líderes operativos dentro de un programa.
/// El líder de programa solo gestiona líderes operativos de los programas que lidera.
/// El administrador no pasa por aquí: su poder es global (ver AdminAccessGuard).
/// </summary>
public static class LeaderAssignmentPolicy
{
    public static bool IsLeadingProgram(IEnumerable<RoleAssignment> actorAssignments, Guid programId, DateTime atUtc) =>
        actorAssignments.Any(a =>
            a.Role == AssignedRole.PROGRAM_LEADER
            && a.Scope == RoleScope.PROGRAM
            && a.ScopeId == programId
            && a.IsActiveAt(atUtc));

    public static bool CanAssign(
        IEnumerable<RoleAssignment> actorAssignments,
        AssignedRole role,
        RoleScope scope,
        Guid? scopeId,
        DateTime atUtc) =>
        role == AssignedRole.OPERATIONAL_LEADER
        && scope == RoleScope.PROGRAM
        && scopeId is { } programId
        && IsLeadingProgram(actorAssignments, programId, atUtc);

    public static bool CanRevoke(IEnumerable<RoleAssignment> actorAssignments, RoleAssignment target, DateTime atUtc) =>
        target.Role == AssignedRole.OPERATIONAL_LEADER
        && target.Scope == RoleScope.PROGRAM
        && target.ScopeId is { } programId
        && IsLeadingProgram(actorAssignments, programId, atUtc);
}
