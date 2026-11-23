namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record ParticipationRequest(List<GroupRequest> Groups, bool AllowMultipleGroups, decimal? Latitude, decimal? Longitude, int? GeofenceRadiusM, bool AllowSelfCheckin, int CheckinToleranceMin);
