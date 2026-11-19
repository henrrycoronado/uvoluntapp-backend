namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record RescheduleActivityResponse(
    ActivityResponse Activity,
    ActivityRescheduleResponse Reschedule,
    IReadOnlyList<ConflictWarning> Conflicts);
