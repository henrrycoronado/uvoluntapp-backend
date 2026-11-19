using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class AccountDirectory(AppDbContext db) : IAccountDirectory
{
    public Task<AccountStatus?> FindStatusAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.Where(u => u.Id == userId).Select(u => (AccountStatus?)u.Status).FirstOrDefaultAsync(cancellationToken);

    public async Task<UserRecord?> FindAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return ToRecord(user, profile);
    }

    public async Task<PagedResult<UserRecord>> SearchAsync(
        AccountStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsQueryable();
        if (status is { } value)
        {
            query = query.Where(u => u.Status == value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(u => u.NormalizedEmail!.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var ids = users.Select(u => u.Id).ToList();
        var profiles = await db.Profiles.Where(p => ids.Contains(p.UserId)).ToListAsync(cancellationToken);
        var items = users.Select(u => ToRecord(u, profiles.FirstOrDefault(p => p.UserId == u.Id))).ToList();
        return new PagedResult<UserRecord>(items, page, pageSize, total);
    }

    public async Task SetVerificationAsync(
        Guid userId,
        DateTime? verifiedAt,
        Guid? verifiedBy,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("La cuenta no existe.");
        user.VerifiedAt = verifiedAt;
        user.VerifiedBy = verifiedAt is null ? null : verifiedBy;
    }

    public async Task<bool> ReplaceAccessEmailAsync(
        Guid userId,
        string newEmail,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var email = newEmail.Trim();
        var normalized = email.ToUpperInvariant();
        var taken = await db.Users.AnyAsync(
            u => u.Id != userId
                && (u.NormalizedEmail == normalized || (u.InstitutionalEmail != null && u.InstitutionalEmail.ToUpper() == normalized)),
            cancellationToken);
        if (taken)
        {
            return false;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("La cuenta no existe.");
        user.Email = email;
        user.UserName = email;
        user.NormalizedEmail = normalized;
        user.NormalizedUserName = normalized;
        user.EmailConfirmed = true;
        user.SecurityStamp = Guid.NewGuid().ToString();
        var sessions = await db.AuthSessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(now);
        }

        return true;
    }

    public async Task SetStatusAsync(
        Guid userId,
        AccountStatus status,
        DateTime? suspendedAt,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("La cuenta no existe.");
        user.Status = status;
        user.SuspendedAt = suspendedAt;
    }

    public async Task RevokeSessionsAsync(
        IReadOnlyList<Guid> sessionIds,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        if (sessionIds.Count == 0)
        {
            return;
        }

        var sessions = await db.AuthSessions.Where(s => sessionIds.Contains(s.Id)).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(revokedAt);
        }
    }

    private static UserRecord ToRecord(AppUser user, Profile? profile) => new(
        user.Id,
        user.Email ?? string.Empty,
        profile?.FirstName ?? string.Empty,
        profile?.LastName ?? string.Empty,
        profile?.Phone,
        user.InstitutionalEmail,
        user.InstitutionalEmailVerifiedAt,
        user.Status,
        user.CreatedAt,
        user.SuspendedAt,
        user.VerifiedAt,
        user.VerifiedBy);
}
