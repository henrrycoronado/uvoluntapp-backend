using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

/// <summary>
/// Crear o editar una actividad (SCRUM-47, 48, 49, 53). En la edición se ignoran programa y periodo:
/// el programa lo cambia solo un ADMIN (SCRUM-123).
/// </summary>
public sealed record ActivityRequest
{
    public Guid? ProgramId { get; init; }

    public Guid? PeriodId { get; init; }

    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required]
    public string Modality { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? Location { get; init; }

    [MaxLength(500)]
    public string? MeetingUrl { get; init; }

    public DateTime StartsAt { get; init; }

    public DateTime EndsAt { get; init; }

    public bool RequiresRegistration { get; init; }

    public int? Capacity { get; init; }

    public decimal? ExpectedHours { get; init; }

    [MaxLength(2000)]
    public string? ParticipationConditions { get; init; }

    [MaxLength(500)]
    public string? PeriodicityNote { get; init; }

    [MaxLength(100)]
    public string? GroupLabel { get; init; }
}
