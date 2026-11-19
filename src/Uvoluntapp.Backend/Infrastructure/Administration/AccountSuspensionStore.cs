using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Accounts.Suspension;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Administration;

public sealed class AccountSuspensionStore(AppDbContext db) : IAccountSuspensionStore
{
    public Task<AccountStatus?> FindStatusAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.Where(u => u.Id == userId).Select(u => (AccountStatus?)u.Status).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AuthSession>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.AuthSessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync(cancellationToken);
}
