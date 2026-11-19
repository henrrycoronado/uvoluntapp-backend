using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Domain.Entities.Activities;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class ActivityStore(AppDbContext db) : IActivityStore
{
    public Task<Activity?> FindAsync(Guid activityId, CancellationToken cancellationToken = default) =>
        db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

    public async Task<PagedResult<Activity>> SearchAsync(ActivitySearch search, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(search.Page, 1);
        var pageSize = Math.Clamp(search.PageSize, 1, 200);
        var query = db.Activities.AsNoTracking().Where(a => a.DeletedAt == null);

        if (search.Statuses is { } statuses)
        {
            query = query.Where(a => statuses.Contains(a.Status));
        }

        // Los borradores solo se ven en el ámbito del actor (o todos, si es ADMIN).
        if (!search.AllDrafts)
        {
            var programIds = search.DraftProgramIds;
            var activityIds = search.DraftActivityIds;
            query = query.Where(a => a.Status != ActivityStatus.DRAFT
                || (a.ProgramId != null && programIds.Contains(a.ProgramId.Value))
                || activityIds.Contains(a.Id));
        }

        if (search.OnlyActivePrograms)
        {
            query = query.Where(a => a.ProgramId == null
                || db.Programs.Any(p => p.Id == a.ProgramId && p.DeletedAt == null && p.Status == ProgramStatus.ACTIVE));
        }

        if (search.ProgramId is { } programId)
        {
            query = query.Where(a => a.ProgramId == programId);
        }

        if (search.PeriodId is { } periodId)
        {
            query = query.Where(a => a.AcademicPeriodId == periodId);
        }

        if (search.Independent is { } independent)
        {
            query = independent ? query.Where(a => a.ProgramId == null) : query.Where(a => a.ProgramId != null);
        }

        if (!string.IsNullOrWhiteSpace(search.GroupLabel))
        {
            var label = search.GroupLabel.Trim().ToLower();
            query = query.Where(a => a.GroupLabel != null && a.GroupLabel.ToLower() == label);
        }

        if (search.From is { } from)
        {
            query = query.Where(a => a.EndsAt >= from);
        }

        if (search.To is { } to)
        {
            query = query.Where(a => a.StartsAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(a => a.StartsAt).ThenBy(a => a.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Activity>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Activity>> ListOverlappingAsync(DateTime startsAt, DateTime endsAt, Guid? excludeActivityId, CancellationToken cancellationToken = default) =>
        await db.Activities.AsNoTracking()
            .Where(a => a.DeletedAt == null
                && (a.Status == ActivityStatus.PUBLISHED || a.Status == ActivityStatus.IN_PROGRESS)
                && a.StartsAt < endsAt
                && a.EndsAt > startsAt
                && a.Id != excludeActivityId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Activity>> ListDueToStartAsync(DateTime nowUtc, int max, CancellationToken cancellationToken = default) =>
        await db.Activities
            .Where(a => a.DeletedAt == null && a.Status == ActivityStatus.PUBLISHED && a.StartsAt <= nowUtc)
            .OrderBy(a => a.StartsAt)
            .Take(max)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ActivityReschedule>> ListReschedulesAsync(Guid activityId, CancellationToken cancellationToken = default) =>
        await db.ActivityReschedules.AsNoTracking().Where(r => r.ActivityId == activityId).OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListIdsForProgramAsync(Guid programId, CancellationToken cancellationToken = default) =>
        await db.Activities.AsNoTracking().Where(a => a.ProgramId == programId).Select(a => a.Id).ToListAsync(cancellationToken);

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default) =>
        await db.Activities.AddAsync(activity, cancellationToken);

    public async Task AddRescheduleAsync(ActivityReschedule reschedule, CancellationToken cancellationToken = default) =>
        await db.ActivityReschedules.AddAsync(reschedule, cancellationToken);
}
