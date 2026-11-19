using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Authorization;

public sealed record AuthorizationRequest(
    bool IsAuthenticated,
    AccountStatus AccountStatus,
    IReadOnlyCollection<RoleAssignment> Assignments,
    AssignedRole RequiredRole,
    RoleScope ResourceScope,
    Guid? ResourceId,
    Guid? ResourceProgramId,
    bool ResourceStateAllowsOperation,
    DateTime? AtUtc = null);
