using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Services;

/// <summary>Lo que el sistema sabe del actor en este instante para decidir permisos.</summary>
public sealed record ActorAccess(
    Guid ActorId,
    bool IsActive,
    bool IsGlobalAdmin,
    IReadOnlyList<RoleAssignment> Assignments,
    DateTime AtUtc)
{
    /// <summary>Gets a value indicating whether líder de programa u operativo con alguna asignación vigente: puede revisar, no editar.</summary>
    public bool IsLeader => Assignments.Any(a =>
        (a.Role == AssignedRole.PROGRAM_LEADER || a.Role == AssignedRole.OPERATIONAL_LEADER)
        && a.IsActiveAt(AtUtc));

    public bool CanReview => IsActive && (IsGlobalAdmin || IsLeader);

    public IReadOnlyList<Guid> LedProgramIds => Assignments
        .Where(a => a.Role == AssignedRole.PROGRAM_LEADER
            && a.Scope == RoleScope.PROGRAM
            && a.ScopeId is not null
            && a.IsActiveAt(AtUtc))
        .Select(a => a.ScopeId!.Value)
        .Distinct()
        .ToList();
}
