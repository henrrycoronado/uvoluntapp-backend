using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record ParticipationResponse(IReadOnlyList<ActivityGroupResponse> Groups, bool AllowMultipleGroups, decimal? Latitude, decimal? Longitude, int? GeofenceRadiusM, bool AllowSelfCheckin, int CheckinToleranceMin)
{
    public static ParticipationResponse From(Activity a) => new(a.Groups.OrderBy(g => g.StartsAt).ThenBy(g => g.Id).Select(ActivityGroupResponse.From).ToList(), a.AllowMultipleGroups, a.Latitude, a.Longitude, a.GeofenceRadiusM, a.AllowSelfCheckin, a.CheckinToleranceMin);
}
