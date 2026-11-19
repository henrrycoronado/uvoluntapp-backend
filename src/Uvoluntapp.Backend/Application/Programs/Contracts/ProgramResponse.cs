using Uvoluntapp.Backend.Domain.Programs;

namespace Uvoluntapp.Backend.Application.Programs.Contracts;

public sealed record ProgramResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static ProgramResponse From(VolunteerProgram program) => new(
        program.Id,
        program.Name,
        program.Description,
        program.Status.ToString(),
        program.CreatedAt,
        program.UpdatedAt);
}
