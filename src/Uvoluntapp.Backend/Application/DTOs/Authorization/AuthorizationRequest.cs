using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Authorization;

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
