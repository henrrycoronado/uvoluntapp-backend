using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class AcademicPeriodStore(AppDbContext db) : IAcademicPeriodStore
{
    public async Task<IReadOnlyList<AcademicPeriod>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.AcademicPeriods.AsNoTracking().Where(p => p.DeletedAt == null).OrderBy(p => p.StartsAt).ToListAsync(cancellationToken);

    public Task<AcademicPeriod?> FindAsync(Guid periodId, CancellationToken cancellationToken = default) =>
        db.AcademicPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId && p.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<AcademicPeriod>> ListForProgramAsync(Guid programId, CancellationToken cancellationToken = default) =>
        await db.AcademicPeriods.AsNoTracking()
            .Where(p => db.Activities.Any(a => a.ProgramId == programId && a.AcademicPeriodId == p.Id && a.DeletedAt == null))
            .OrderBy(p => p.StartsAt)
            .ToListAsync(cancellationToken);

    public Task<AcademicPeriod?> ActiveAsync(CancellationToken ct) => db.AcademicPeriods.SingleOrDefaultAsync(p => p.IsActive, ct);

    public async Task<AcademicPeriod> ChangeAsync(Guid periodId, string label, DateTime? end, DateTime today, bool close, Func<AcademicPeriod, Task> record, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM academic_periods WHERE id = {periodId} FOR UPDATE", ct);
        var period = await db.AcademicPeriods.FindAsync([periodId], ct) ?? throw new NotFoundException("El periodo no existe.");
        if (close)
        {
            var existing = await db.AcademicPeriods.SingleOrDefaultAsync(p => p.PreviousPeriodId == periodId, ct);
            if (existing != null)
            {
                return existing;
            }

            if (await db.Activities.AnyAsync(a => a.AcademicPeriodId == periodId && a.DeletedAt == null && a.Status != ActivityStatus.FINISHED && a.Status != ActivityStatus.CANCELLED, ct))
            {
                throw new ConflictException("Quedan actividades pendientes en el periodo.");
            }

            var successor = period.Close(label, today);
            await db.SaveChangesAsync(ct);
            db.AcademicPeriods.Add(successor);
            period = successor;
        }
        else
        {
            period.Edit(label, end, today);
        }

        await record(period);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return period;
    }
}
