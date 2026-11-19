using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Programs;

public sealed class ProgramPeriod : Entity
{
    public Guid ProgramId { get; set; }

    public Guid AcademicPeriodId { get; set; }
}
