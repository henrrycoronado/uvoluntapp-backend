namespace Uvoluntapp.Backend.Domain.Programs;

public sealed class ProgramPeriod : Entity
{
    public Guid ProgramId { get; set; }

    public Guid AcademicPeriodId { get; set; }
}
