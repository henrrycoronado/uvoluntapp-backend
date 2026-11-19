namespace Uvoluntapp.Backend.Domain.Activities;

public sealed class Activity : Entity
{
    public Guid? ProgramId { get; set; }

    public Guid AcademicPeriodId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public required ActivityModality Modality { get; set; }

    public string? Location { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public int? Capacity { get; set; }

    public string? PeriodicityNote { get; set; }

    public string? CancellationReason { get; private set; }

    public ActivityStatus Status { get; private set; } = ActivityStatus.DRAFT;

    public DateTime? StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public void Publish()
    {
        if (Status != ActivityStatus.DRAFT)
        {
            throw new InvalidOperationException();
        }

        Status = ActivityStatus.PUBLISHED;
    }

    public void Cancel(string reason)
    {
        if (Status is not (ActivityStatus.DRAFT or ActivityStatus.PUBLISHED) || string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException();
        }

        CancellationReason = reason;
        Status = ActivityStatus.CANCELLED;
    }

    public void Finish()
    {
        if (Status != ActivityStatus.IN_PROGRESS)
        {
            throw new InvalidOperationException();
        }

        Status = ActivityStatus.FINISHED;
        FinishedAt = DateTime.UtcNow;
    }
}
