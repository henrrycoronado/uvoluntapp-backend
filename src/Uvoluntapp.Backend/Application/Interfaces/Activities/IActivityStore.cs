using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.Interfaces.Activities;

public interface IActivityStore
{
    Task<Activity?> FindAsync(Guid activityId, CancellationToken cancellationToken = default);

    Task<PagedResult<Activity>> SearchAsync(ActivitySearch search, CancellationToken cancellationToken = default);

    // Publicadas o en ejecución que se cruzan con el intervalo (SCRUM-54).
    Task<IReadOnlyList<Activity>> ListOverlappingAsync(DateTime startsAt, DateTime endsAt, Guid? excludeActivityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Activity>> ListDueToStartAsync(DateTime nowUtc, int max, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityReschedule>> ListReschedulesAsync(Guid activityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListIdsForProgramAsync(Guid programId, CancellationToken cancellationToken = default);

    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);

    Task AddRescheduleAsync(ActivityReschedule reschedule, CancellationToken cancellationToken = default);
}
