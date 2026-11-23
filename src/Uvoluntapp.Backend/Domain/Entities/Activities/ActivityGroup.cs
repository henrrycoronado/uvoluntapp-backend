using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Domain.Entities.Activities;

public sealed class ActivityGroup : Entity
{
    public Guid ActivityId { get; init; }

    public required string Name { get; init; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public int? Capacity { get; init; }

    public void Validate(DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 100 || StartsAt < start || EndsAt > end || EndsAt <= StartsAt || Capacity < 0)
        {
            throw new DomainRuleViolationException("El grupo necesita nombre, horario dentro de la actividad y cupo no negativo o vacío.");
        }
    }
}
