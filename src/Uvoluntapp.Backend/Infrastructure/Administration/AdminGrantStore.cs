using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Administration.Ports;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Administration;

public sealed class AdminGrantStore(AppDbContext db) : IAdminGrantStore
{
    public Task<AdminGrantRequest?> FindRequestAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        db.AdminGrantRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

    public async Task<PagedResult<AdminGrantRequest>> SearchRequestsAsync(
        AdminGrantStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.AdminGrantRequests.AsQueryable();
        if (status is { } value)
        {
            query = query.Where(r => r.Status == value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminGrantRequest>(items, page, pageSize, total);
    }

    public Task<bool> HasPendingRequestAsync(Guid beneficiaryId, CancellationToken cancellationToken = default) =>
        db.AdminGrantRequests.AnyAsync(r => r.BeneficiaryId == beneficiaryId && r.Status == AdminGrantStatus.PENDING, cancellationToken);

    public async Task<IReadOnlyList<AdminGrantApproval>> ListApprovalsAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        await db.AdminGrantApprovals
            .Where(a => a.RequestId == requestId)
            .OrderBy(a => a.ApprovedAt)
            .ToListAsync(cancellationToken);

    public async Task AddRequestAsync(AdminGrantRequest request, CancellationToken cancellationToken = default) =>
        await db.AdminGrantRequests.AddAsync(request, cancellationToken);

    public async Task AddApprovalAsync(AdminGrantApproval approval, CancellationToken cancellationToken = default) =>
        await db.AdminGrantApprovals.AddAsync(approval, cancellationToken);
}
