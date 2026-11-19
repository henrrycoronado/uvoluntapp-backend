using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Activities;

public sealed class ActivityReschedule : Entity
{
    public Guid ActivityId { get; set; }

    public Guid AuthorId { get; set; }

    public DateTime PreviousStartsAt { get; set; }

    public DateTime PreviousEndsAt { get; set; }

    public DateTime NewStartsAt { get; set; }

    public DateTime NewEndsAt { get; set; }

    public string Reason { get; set; } = string.Empty;
}
