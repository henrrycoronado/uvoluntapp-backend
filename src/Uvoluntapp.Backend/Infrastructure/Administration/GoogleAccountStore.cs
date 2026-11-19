using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Accounts.Google;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Administration;

public sealed class GoogleAccountStore(AppDbContext db) : IGoogleAccountStore
{
    private const string Provider = "google";

    public async Task<GoogleAccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
        return user is null ? null : ToRecord(user);
    }

    public async Task<GoogleAccountRecord?> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        var userId = await db.UserLogins
            .Where(l => l.LoginProvider == Provider && l.ProviderKey == subject)
            .Select(l => l.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId == Guid.Empty)
        {
            return null;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? null : ToRecord(user);
    }

    public async Task<GoogleAccountRecord> CreateAsync(
        GoogleIdentity identity,
        AccountStatus status,
        bool institutionalEmailVerified,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = identity.Email,
            NormalizedUserName = identity.Email.ToUpperInvariant(),
            Email = identity.Email,
            NormalizedEmail = identity.Email.ToUpperInvariant(),
            EmailConfirmed = identity.EmailVerified,
            Status = status,
            InstitutionalEmail = institutionalEmailVerified ? identity.Email : null,
            InstitutionalEmailVerifiedAt = institutionalEmailVerified ? now : null,
            CreatedAt = now,
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        await db.Users.AddAsync(user, cancellationToken);
        await db.Profiles.AddAsync(
            new Profile { UserId = user.Id, FirstName = identity.GivenName ?? string.Empty, LastName = identity.FamilyName ?? string.Empty },
            cancellationToken);
        var login = new IdentityUserLogin<Guid>
        {
            LoginProvider = Provider,
            ProviderKey = identity.Subject,
            ProviderDisplayName = "Google",
            UserId = user.Id,
        };
        await db.UserLogins.AddAsync(login, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToRecord(user);
    }

    public Task<bool> HasApprovalRequestAsync(Guid userId, CancellationToken cancellationToken) =>
        db.AccountApprovalRequests.AnyAsync(r => r.UserId == userId, cancellationToken);

    public async Task LinkAsync(Guid userId, GoogleIdentity identity, CancellationToken cancellationToken)
    {
        var login = new IdentityUserLogin<Guid>
        {
            LoginProvider = Provider,
            ProviderKey = identity.Subject,
            ProviderDisplayName = "Google",
            UserId = userId,
        };
        await db.UserLogins.AddAsync(login, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static GoogleAccountRecord ToRecord(AppUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.EmailConfirmed, user.Status);
}
