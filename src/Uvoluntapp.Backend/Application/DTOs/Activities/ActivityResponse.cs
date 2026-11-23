using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record ActivityResponse(
    Guid Id,
    Guid? ProgramId,
    string? ProgramName,
    Guid PeriodId,
    string Title,
    string? Description,
    string Modality,
    string? Location,
    string? MeetingUrl,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RequiresRegistration,
    int? Capacity,
    decimal? ExpectedHours,
    string? ParticipationConditions,
    ParticipationResponse Participation,
    string? GroupLabel,
    string Status,
    string? CancellationReason,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool CanManage = false,
    bool ImagePresent = false)
{
    public string? ImageUrl => ImagePresent ? $"/api/v1/activities/{Id}/image" : null;

    public static ActivityResponse From(Activity activity, string? programName, bool canManage) => new(
        activity.Id,
        activity.ProgramId,
        programName,
        activity.AcademicPeriodId,
        activity.Title,
        activity.Description,
        activity.Modality.ToString(),
        activity.Location,
        activity.MeetingUrl,
        activity.StartsAt,
        activity.EndsAt,
        activity.RequiresRegistration,
        activity.Capacity,
        activity.ExpectedHours,
        activity.ParticipationConditions ?? "Ninguna",
        ParticipationResponse.From(activity),
        activity.GroupLabel,
        activity.Status.ToString(),
        activity.CancellationReason,
        activity.StartedAt,
        activity.FinishedAt,
        activity.CreatedAt,
        activity.UpdatedAt,
        canManage,
        activity.Images.Count > 0);
}
