using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Application.Programs.Ports;
using Uvoluntapp.Backend.Domain.Programs;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Programs;

public sealed class ProgramReader(AppDbContext db) : IProgramReader
{
    public async Task<PagedResult<VolunteerProgram>> SearchAsync(
        ProgramStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Programs.AsNoTracking().Where(p => p.DeletedAt == null);
        if (status is { } value)
        {
            query = query.Where(p => p.Status == value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<VolunteerProgram>(items, page, pageSize, total);
    }
}
