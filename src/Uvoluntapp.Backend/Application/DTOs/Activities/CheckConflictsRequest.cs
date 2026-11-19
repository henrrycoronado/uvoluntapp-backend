namespace Uvoluntapp.Backend.Application.DTOs.Activities;

/// <summary>Comprobar conflictos antes de crear o reprogramar (SCRUM-54).</summary>
public sealed record CheckConflictsRequest
{
    public Guid? ActivityId { get; init; }

    // Para una actividad nueva: su programa define los responsables.
    public Guid? ProgramId { get; init; }

    public DateTime StartsAt { get; init; }

    public DateTime EndsAt { get; init; }

    public string? Location { get; init; }
}
