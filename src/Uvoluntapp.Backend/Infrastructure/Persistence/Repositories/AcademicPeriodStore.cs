using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Domain.Entities.Programs;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class AcademicPeriodStore(AppDbContext db) : IAcademicPeriodStore
{
    public async Task<IReadOnlyList<AcademicPeriod>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.AcademicPeriods.AsNoTracking().Where(p => p.DeletedAt == null).OrderBy(p => p.StartsAt).ToListAsync(cancellationToken);

    public Task<AcademicPeriod?> FindAsync(Guid periodId, CancellationToken cancellationToken = default) =>
        db.AcademicPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId && p.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<AcademicPeriod>> ListForProgramAsync(Guid programId, CancellationToken cancellationToken = default) =>
        await db.AcademicPeriods.AsNoTracking()
            .Where(p => db.ProgramPeriods.Any(link => link.ProgramId == programId && link.AcademicPeriodId == p.Id && link.DeletedAt == null))
            .OrderBy(p => p.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task EnsureProgramLinkAsync(Guid programId, Guid periodId, CancellationToken cancellationToken = default)
    {
        var tracked = db.ProgramPeriods.Local.Any(link => link.ProgramId == programId && link.AcademicPeriodId == periodId);
        if (tracked || await db.ProgramPeriods.AnyAsync(link => link.ProgramId == programId && link.AcademicPeriodId == periodId, cancellationToken))
        {
            return;
        }

        await db.ProgramPeriods.AddAsync(new ProgramPeriod { ProgramId = programId, AcademicPeriodId = periodId }, cancellationToken);
    }
}
