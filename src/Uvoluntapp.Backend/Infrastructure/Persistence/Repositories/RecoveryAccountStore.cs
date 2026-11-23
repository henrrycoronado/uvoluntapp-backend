using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Audit;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Infrastructure.Identity;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class RecoveryAccountStore(AppDbContext db, UserManager<AppUser> users) : IRecoveryAccountStore
{
    public Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.Where(u => u.NormalizedEmail == normalizedEmail).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task SaveTokenAsync(PasswordRecoveryToken token, CancellationToken cancellationToken)
    {
        await db.PasswordRecoveryTokens.AddAsync(token, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<PasswordRecoveryToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.PasswordRecoveryTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash && (t.NormalizedEmail == null || db.Users.Any(u => u.Id == t.UserId && u.NormalizedEmail == t.NormalizedEmail)),
            cancellationToken);

    public async Task SetPasswordAsync(PasswordRecoveryToken claimed, string newPassword, DateTime utcNow, CancellationToken cancellationToken)
    {
        var userId = claimed.UserId;
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("La cuenta no existe.");

        if (claimed.NormalizedEmail != null && claimed.NormalizedEmail != user.NormalizedEmail)
        {
            throw new AccountException(AccountError.InvalidCode);
        }

        // Se valida antes de quitar la contraseña actual: un rechazo no deja la cuenta sin contraseña.
        foreach (var validator in users.PasswordValidators)
        {
            var valid = await validator.ValidateAsync(users, user, newPassword);
            if (!valid.Succeeded)
            {
                throw new ArgumentException(string.Join(' ', valid.Errors.Select(e => e.Description)), nameof(newPassword));
            }
        }

        if (!user.EmailConfirmed && user.Status == Uvoluntapp.Backend.Domain.Utils.Enums.AccountStatus.ACTIVE)
        {
            user.EmailConfirmed = true;
            if (user.Email != null && InstitutionalEmailDomain.IsInstitutional(user.Email))
            {
                user.InstitutionalEmail = user.Email;
                user.VerifiedAt = utcNow;
                user.InstitutionalEmailVerifiedAt = utcNow;
                user.VerifiedBy = null;
            }
        }

        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.PasswordHash = users.PasswordHasher.HashPassword(user, newPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        var sessions = await db.AuthSessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(utcNow);
        }

        db.AuditEvents.Add(AuditEvent.Create(
            userId,
            AuditActions.PasswordReset,
            AuditResourceTypes.User,
            userId,
            new Dictionary<string, object?> { ["revokedSessions"] = sessions.Count },
            utcNow));
        await db.SaveChangesAsync(cancellationToken);
    }
}
