using Uvoluntapp.Backend.Domain.Entities.Programs;

namespace Uvoluntapp.Backend.Application.Interfaces.Programs;

public interface IAcademicPeriodStore
{
    Task<IReadOnlyList<AcademicPeriod>> ListAsync(CancellationToken cancellationToken = default);

    Task<AcademicPeriod?> FindAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicPeriod>> ListForProgramAsync(Guid programId, CancellationToken cancellationToken = default);

    // Crea el vínculo programa–periodo si aún no existe (SCRUM-46: sin gestión manual en M2).
    Task EnsureProgramLinkAsync(Guid programId, Guid periodId, CancellationToken cancellationToken = default);
}
