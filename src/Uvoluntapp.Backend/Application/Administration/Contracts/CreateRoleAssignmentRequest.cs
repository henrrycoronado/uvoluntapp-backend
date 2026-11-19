namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record CreateRoleAssignmentRequest(
    string UserId,
    string Role,
    string Scope,
    string? ScopeId,
    DateTime? ValidFrom,
    DateTime? ValidUntil);
