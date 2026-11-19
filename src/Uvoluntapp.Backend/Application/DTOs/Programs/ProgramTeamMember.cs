namespace Uvoluntapp.Backend.Application.DTOs.Programs;

/// <summary>Integrante del equipo de un programa: líder del programa o líder operativo, y sus delegaciones (SCRUM-122).</summary>
public sealed record ProgramTeamMember(
    Guid AssignmentId,
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    string Scope,
    Guid ScopeId,
    string? ActivityTitle,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    Guid GrantedBy);
