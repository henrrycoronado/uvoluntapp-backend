using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record RoleAssignmentResponse(
    Guid Id,
    Guid UserId,
    string Role,
    string Scope,
    Guid? ScopeId,
    Guid GrantedBy,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    DateTime? RevokedAt,
    DateTime CreatedAt)
{
    public static RoleAssignmentResponse From(RoleAssignment assignment) => new(
        assignment.Id,
        assignment.UserId,
        assignment.Role.ToString(),
        assignment.Scope.ToString(),
        assignment.ScopeId,
        assignment.GrantedBy,
        assignment.ValidFrom,
        assignment.ValidUntil,
        assignment.RevokedAt,
        assignment.CreatedAt);
}
