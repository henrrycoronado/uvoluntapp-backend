namespace Uvoluntapp.Backend.Application.DTOs.Administration;

public sealed record CreateRoleAssignmentRequest(
    string UserId,
    string Role,
    string Scope,
    string? ScopeId,
    DateTime? ValidFrom,
    DateTime? ValidUntil);
