namespace Uvoluntapp.Backend.Domain.Programs;

public sealed class Program : Entity
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
