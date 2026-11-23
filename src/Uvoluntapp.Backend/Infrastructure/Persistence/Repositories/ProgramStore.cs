using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class ProgramStore(AppDbContext db) : IProgramStore
{
    public Task<VolunteerProgram?> FindAsync(Guid programId, CancellationToken cancellationToken = default) =>
        db.Programs.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);

    public async Task<PagedResult<VolunteerProgram>> SearchAsync(
        IReadOnlyCollection<ProgramStatus>? statuses,
        bool includeArchived,
        Guid? periodId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Programs.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(p => p.DeletedAt == null);
        }

        if (statuses is not null)
        {
            query = query.Where(p => statuses.Contains(p.Status));
        }

        if (periodId is { } period)
        {
            query = query.Where(p => db.Activities.Any(link => link.ProgramId == p.Id && link.AcademicPeriodId == period && link.DeletedAt == null));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<VolunteerProgram>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken = default) =>
        programIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Programs.AsNoTracking().Where(p => programIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

    public Task<bool> HasLiveActivitiesAsync(Guid programId, CancellationToken cancellationToken = default) =>
        db.Activities.AnyAsync(
            a => a.ProgramId == programId && a.DeletedAt == null && (a.Status == ActivityStatus.PUBLISHED || a.Status == ActivityStatus.IN_PROGRESS),
            cancellationToken);

    public async Task AddAsync(VolunteerProgram program, CancellationToken cancellationToken = default) =>
        await db.Programs.AddAsync(program, cancellationToken);
}
