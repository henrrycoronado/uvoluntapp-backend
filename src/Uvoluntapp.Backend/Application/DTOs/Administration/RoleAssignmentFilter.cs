using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Administration;

/// <summary>Filtro de asignaciones. <see cref="ScopeIds"/> limita a ciertos programas o actividades (vista del líder).</summary>
public sealed record RoleAssignmentFilter(
    Guid? UserId,
    AssignedRole? Role,
    RoleScope? Scope,
    bool? Active,
    DateTime AtUtc,
    int Page,
    int PageSize,
    IReadOnlyCollection<Guid>? ScopeIds = null);
