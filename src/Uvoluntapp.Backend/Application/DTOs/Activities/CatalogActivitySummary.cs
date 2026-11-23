using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

/// <summary>Actividad en el catálogo público: sin enlace de reunión ni datos internos (SCRUM-49, SCRUM-52).</summary>
public sealed record CatalogActivitySummary(
    Guid Id,
    string Title,
    string? Description,
    Guid? ProgramId,
    string? ProgramName,
    string? GroupLabel,
    Guid PeriodId,
    string Modality,
    string? Location,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresRegistration,
    int? Capacity,
    decimal? ExpectedHours,
    string? ParticipationConditions,
    ParticipationResponse Participation,
    string Status,
    bool ImagePresent = false)
{
    public string? ImageUrl => ImagePresent ? $"/api/v1/activities/{Id}/image" : null;

    public static CatalogActivitySummary From(Activity activity, string? programName) => new(
        activity.Id,
        activity.Title,
        activity.Description,
        activity.ProgramId,
        programName,
        activity.GroupLabel,
        activity.AcademicPeriodId,
        activity.Modality.ToString(),
        activity.Location,
        activity.StartsAt,
        activity.EndsAt,
        activity.RequiresRegistration,
        activity.Capacity,
        activity.ExpectedHours,
        activity.ParticipationConditions ?? "Ninguna",
        ParticipationResponse.From(activity),
        activity.Status.ToString(),
        activity.Images.Count > 0);
}
