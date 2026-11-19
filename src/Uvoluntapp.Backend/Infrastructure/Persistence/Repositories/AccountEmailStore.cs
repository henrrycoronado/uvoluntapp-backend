using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence.Records;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class AccountEmailStore(AppDbContext db, UserManager<AppUser> users) : IAccountEmailStore
{
    public async Task<AccountEmail?> FindAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? null : new AccountEmail
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            EmailVerifiedAt = user.EmailConfirmed ? user.CreatedAt : null,
            InstitutionalEmail = user.InstitutionalEmail,
            InstitutionalEmailVerifiedAt = user.InstitutionalEmailVerifiedAt,
        };
    }

    public async Task<bool> IsEmailInUseAsync(string email, Guid exceptUserId, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        Expression<Func<AppUser, bool>> inUse = u => u.Id != exceptUserId
            && (u.NormalizedEmail == normalized
                || (u.InstitutionalEmail != null && u.InstitutionalEmail.ToUpper() == normalized));
        return await db.Users.AnyAsync(inUse, cancellationToken);
    }

    public async Task<bool> VerifyPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is not null && await users.CheckPasswordAsync(user, password);
    }

    public async Task<IReadOnlyList<AuthSession>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.AuthSessions.Where(s => s.UserId == userId).ToListAsync(cancellationToken);

    public async Task<PendingEmailVerification?> FindPendingAsync(
        Guid userId,
        EmailVerificationPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PendingEmailVerifications
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Purpose == purpose, cancellationToken);
        return record is null ? null : PendingEmailVerification.Restore(
            record.Email,
            record.Purpose,
            OneTimeCode.Restore(record.CodeHash, record.ExpiresAt, record.Attempts, record.IsUsed),
            record.RequestedAt);
    }

    public async Task SaveAsync(AccountEmail account, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == account.UserId, cancellationToken)
            ?? throw new NotFoundException("La cuenta no existe.");
        user.Email = account.Email;
        user.NormalizedEmail = account.Email.ToUpperInvariant();
        user.UserName = account.Email;
        user.NormalizedUserName = account.Email.ToUpperInvariant();
        user.EmailConfirmed = account.EmailVerifiedAt is not null;
        user.InstitutionalEmail = account.InstitutionalEmail;
        user.InstitutionalEmailVerifiedAt = account.InstitutionalEmailVerifiedAt;
    }

    public async Task SavePendingAsync(Guid userId, PendingEmailVerification pending, CancellationToken cancellationToken = default)
    {
        await RemovePendingAsync(userId, pending.Purpose, cancellationToken);
        var record = new PendingEmailVerificationRecord
        {
            UserId = userId,
            Email = pending.Email,
            Purpose = pending.Purpose,
            CodeHash = pending.Code.CodeHash,
            ExpiresAt = pending.Code.ExpiresAt,
            Attempts = pending.Code.Attempts,
            IsUsed = pending.Code.IsUsed,
            RequestedAt = pending.RequestedAt,
        };
        await db.PendingEmailVerifications.AddAsync(record, cancellationToken);
    }

    public async Task RemovePendingAsync(Guid userId, EmailVerificationPurpose purpose, CancellationToken cancellationToken = default)
    {
        var existing = await db.PendingEmailVerifications
            .Where(p => p.UserId == userId && p.Purpose == purpose)
            .ToListAsync(cancellationToken);
        db.PendingEmailVerifications.RemoveRange(existing);
    }
}
