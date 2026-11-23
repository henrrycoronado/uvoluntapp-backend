using Uvoluntapp.Backend.Domain.Entities.Programs;

namespace Uvoluntapp.Backend.Application.Interfaces.Programs;

public interface IAcademicPeriodStore
{
    Task<IReadOnlyList<AcademicPeriod>> ListAsync(CancellationToken cancellationToken = default);

    Task<AcademicPeriod?> FindAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicPeriod>> ListForProgramAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<AcademicPeriod?> ActiveAsync(CancellationToken ct);

    Task<AcademicPeriod> ChangeAsync(Guid periodId, string label, DateTime? end, DateTime today, bool close, Func<AcademicPeriod, Task> record, CancellationToken ct);
}
