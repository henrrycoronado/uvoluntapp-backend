using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Entities.Programs;

public sealed class VolunteerProgram : Entity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ProgramStatus Status { get; private set; } = ProgramStatus.DRAFT;

    public void Activate()
    {
        if (Status is not (ProgramStatus.DRAFT or ProgramStatus.INACTIVE))
        {
            throw new InvalidOperationException();
        }

        Status = ProgramStatus.ACTIVE;
    }

    public void Deactivate()
    {
        if (Status != ProgramStatus.ACTIVE)
        {
            throw new InvalidOperationException();
        }

        Status = ProgramStatus.INACTIVE;
    }
}
