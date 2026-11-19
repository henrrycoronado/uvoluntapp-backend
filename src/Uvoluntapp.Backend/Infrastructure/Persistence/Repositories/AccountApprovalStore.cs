using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class AccountApprovalStore(AppDbContext db) : IAccountApprovalStore
{
    public Task<AccountApprovalRequest?> FindAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        db.AccountApprovalRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

    public async Task<PagedResult<ApprovalRequestView>> SearchAsync(
        AccountApprovalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.AccountApprovalRequests.Where(r => true);
        if (status is { } value)
        {
            query = query.Where(r => r.Status == value);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { Request = r, Email = db.Users.Where(u => u.Id == r.UserId).Select(u => u.Email).FirstOrDefault() })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => new ApprovalRequestView(row.Request, row.Email ?? string.Empty)).ToList();
        return new PagedResult<ApprovalRequestView>(items, page, pageSize, total);
    }
}
