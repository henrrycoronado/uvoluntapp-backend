using Uvoluntapp.Backend.Domain.Entities.Programs;

namespace Uvoluntapp.Backend.Application.DTOs.Programs;

/// <summary>Ficha del programa. <see cref="CanManage"/> indica si el actor puede gestionarlo (para la web).</summary>
public sealed record ProgramResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Contact,
    string? ReferenceSchedule,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt,
    bool CanManage = false,
    bool ImagePresent = false)
{
    public string? ImageUrl => ImagePresent ? $"/api/v1/programs/{Id}/cover" : null;

    public static ProgramResponse From(VolunteerProgram program, bool canManage = false) => new(
        program.Id,
        program.Name,
        program.Description,
        program.Contact,
        program.ReferenceSchedule,
        program.Status.ToString(),
        program.CreatedAt,
        program.UpdatedAt,
        program.DeletedAt,
        canManage,
        program.Images.Count > 0);
}
