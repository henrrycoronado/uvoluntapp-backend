using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Activities;

public sealed class Enrollment : Entity
{
    public Guid ActivityId { get; init; }

    public Guid? GroupId { get; init; }

    public Guid UserId { get; init; }

    public DateTime? CancelledAt { get; private set; }

    public void Cancel(DateTime now) => CancelledAt ??= now;
}
