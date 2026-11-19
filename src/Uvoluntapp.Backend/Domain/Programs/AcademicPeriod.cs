namespace Uvoluntapp.Backend.Domain.Programs;

public sealed class AcademicPeriod : Entity
{
    public string Label { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }
}
