using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.Interfaces.Administration;

public interface IRoleAssignmentStore
{
    Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RoleAssignment?> FindAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    Task<PagedResult<RoleAssignment>> SearchAsync(RoleAssignmentFilter filter, CancellationToken cancellationToken = default);

    Task<int> CountActiveAdminAssignmentsAsync(DateTime atUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListActiveAdminUserIdsAsync(DateTime atUtc, CancellationToken cancellationToken = default);

    // Asignaciones vigentes de líderes sobre esos programas o actividades (responsables, SCRUM-54).
    Task<IReadOnlyList<RoleAssignment>> ListActiveForScopesAsync(
        IReadOnlyCollection<Guid> programIds,
        IReadOnlyCollection<Guid> activityIds,
        DateTime atUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(RoleAssignment assignment, CancellationToken cancellationToken = default);
}
