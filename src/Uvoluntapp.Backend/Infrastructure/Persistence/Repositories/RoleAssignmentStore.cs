using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class RoleAssignmentStore(AppDbContext db) : IRoleAssignmentStore
{
    public async Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.RoleAssignments.Where(r => r.UserId == userId).ToListAsync(cancellationToken);

    public Task<RoleAssignment?> FindAsync(Guid assignmentId, CancellationToken cancellationToken = default) =>
        db.RoleAssignments.FirstOrDefaultAsync(r => r.Id == assignmentId, cancellationToken);

    public async Task<PagedResult<RoleAssignment>> SearchAsync(RoleAssignmentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.RoleAssignments.AsQueryable();
        if (filter.UserId is { } userId)
        {
            query = query.Where(r => r.UserId == userId);
        }

        if (filter.Role is { } role)
        {
            query = query.Where(r => r.Role == role);
        }

        if (filter.Scope is { } scope)
        {
            query = query.Where(r => r.Scope == scope);
        }

        if (filter.ScopeIds is { } scopeIds)
        {
            var ids = scopeIds.ToList();
            query = query.Where(r => r.ScopeId != null && ids.Contains(r.ScopeId.Value));
        }

        var at = filter.AtUtc;
        if (filter.Active is { } active)
        {
            Expression<Func<RoleAssignment, bool>> activeNow = r => r.RevokedAt == null
                && (r.ValidFrom == null || r.ValidFrom <= at)
                && (r.ValidUntil == null || r.ValidUntil > at);
            query = active
                ? query.Where(activeNow)
                : query.Where(r => !(r.RevokedAt == null
                    && (r.ValidFrom == null || r.ValidFrom <= at)
                    && (r.ValidUntil == null || r.ValidUntil > at)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<RoleAssignment>(items, filter.Page, filter.PageSize, total);
    }

    public Task<int> CountActiveAdminAssignmentsAsync(DateTime atUtc, CancellationToken cancellationToken = default)
    {
        Expression<Func<RoleAssignment, bool>> isActiveAdmin = r =>
            r.Role == AssignedRole.ADMIN
            && r.RevokedAt == null
            && (r.ValidFrom == null || r.ValidFrom <= atUtc)
            && (r.ValidUntil == null || r.ValidUntil > atUtc);
        return db.RoleAssignments.CountAsync(isActiveAdmin, cancellationToken);
    }

    public async Task<IReadOnlyList<RoleAssignment>> ListActiveForScopesAsync(
        IReadOnlyCollection<Guid> programIds,
        IReadOnlyCollection<Guid> activityIds,
        DateTime atUtc,
        CancellationToken cancellationToken = default)
    {
        if (programIds.Count == 0 && activityIds.Count == 0)
        {
            return [];
        }

        return await db.RoleAssignments
            .Where(r => r.Role != AssignedRole.ADMIN
                && r.RevokedAt == null
                && (r.ValidFrom == null || r.ValidFrom <= atUtc)
                && (r.ValidUntil == null || r.ValidUntil > atUtc)
                && r.ScopeId != null
                && ((r.Scope == RoleScope.PROGRAM && programIds.Contains(r.ScopeId.Value))
                    || (r.Scope == RoleScope.ACTIVITY && activityIds.Contains(r.ScopeId.Value))))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListActiveAdminUserIdsAsync(DateTime atUtc, CancellationToken cancellationToken = default) =>
        await db.RoleAssignments
            .Where(r => r.Role == AssignedRole.ADMIN
                && r.RevokedAt == null
                && (r.ValidFrom == null || r.ValidFrom <= atUtc)
                && (r.ValidUntil == null || r.ValidUntil > atUtc)
                && db.Users.Any(u => u.Id == r.UserId && u.Status == AccountStatus.ACTIVE))
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RoleAssignment assignment, CancellationToken cancellationToken = default) =>
        await db.RoleAssignments.AddAsync(assignment, cancellationToken);
}
