using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Entities.Activities;

/// <summary>Datos de planificación y configuración de una actividad (SCRUM-48, SCRUM-49).</summary>
public sealed record ActivityPlan(
    string Title,
    string? Description,
    ActivityModality Modality,
    string? Location,
    string? MeetingUrl,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresRegistration,
    int? Capacity,
    decimal? ExpectedHours,
    string? ParticipationConditions,
    string? GroupLabel);
