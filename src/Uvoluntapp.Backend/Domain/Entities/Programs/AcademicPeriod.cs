using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Domain.Entities.Programs;

public sealed class AcademicPeriod : Entity
{
    public bool IsActive { get; set; }

    public Guid? PreviousPeriodId { get; init; }

    public string Label { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public void Edit(string label, DateTime? end, DateTime today)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(label) || label.Trim().Length > 20 || end?.Date < today.Date || end?.Date < StartsAt.Date)
        {
            throw new DomainRuleViolationException("Se necesita un periodo activo, nombre válido y una fecha final vigente.");
        }

        Label = label.Trim();
        EndsAt = end?.Date;
    }

    public AcademicPeriod Close(string successorLabel, DateTime today)
    {
        if (!IsActive || EndsAt == null || EndsAt.Value.Date > today.Date || today.Date < StartsAt.Date)
        {
            throw new DomainRuleViolationException("El periodo aún no puede cerrarse.");
        }

        var successor = new AcademicPeriod { StartsAt = today.Date, IsActive = true, PreviousPeriodId = Id };
        successor.Edit(successorLabel, null, today);
        IsActive = false;
        EndsAt = today.Date;
        return successor;
    }
}
