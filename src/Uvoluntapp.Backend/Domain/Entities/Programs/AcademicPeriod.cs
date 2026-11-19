using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Programs;

public sealed class AcademicPeriod : Entity
{
    public string Label { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }
}
