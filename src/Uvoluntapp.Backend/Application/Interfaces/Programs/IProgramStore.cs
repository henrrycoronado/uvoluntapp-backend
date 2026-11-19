using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Programs;

public interface IProgramStore
{
    Task<VolunteerProgram?> FindAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<PagedResult<VolunteerProgram>> SearchAsync(
        IReadOnlyCollection<ProgramStatus>? statuses,
        bool includeArchived,
        Guid? periodId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken = default);

    // Actividades publicadas o en ejecución impiden desactivar o archivar (SCRUM-103).
    Task<bool> HasLiveActivitiesAsync(Guid programId, CancellationToken cancellationToken = default);

    Task AddAsync(VolunteerProgram program, CancellationToken cancellationToken = default);
}
