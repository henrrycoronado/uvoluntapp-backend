namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record ConflictWarning(string Type, Guid ConflictingActivityId, string ConflictingActivityTitle, string Message);
