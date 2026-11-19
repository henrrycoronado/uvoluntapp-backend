using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Ports;

public sealed record RoleAssignmentFilter(
    Guid? UserId,
    AssignedRole? Role,
    RoleScope? Scope,
    bool? Active,
    DateTime AtUtc,
    int Page,
    int PageSize);
