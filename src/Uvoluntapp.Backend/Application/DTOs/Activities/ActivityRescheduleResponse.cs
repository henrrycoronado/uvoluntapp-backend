using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record ActivityRescheduleResponse(
    Guid Id,
    Guid ActivityId,
    string Reason,
    DateTime PreviousStartsAt,
    DateTime PreviousEndsAt,
    DateTime NewStartsAt,
    DateTime NewEndsAt,
    Guid RescheduledBy,
    DateTime CreatedAt)
{
    public static ActivityRescheduleResponse From(ActivityReschedule record) => new(
        record.Id,
        record.ActivityId,
        record.Reason,
        record.PreviousStartsAt,
        record.PreviousEndsAt,
        record.NewStartsAt,
        record.NewEndsAt,
        record.AuthorId,
        record.CreatedAt);
}
