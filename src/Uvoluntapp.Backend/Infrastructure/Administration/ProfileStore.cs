using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Profiles.Ports;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Administration;

public sealed class ProfileStore(AppDbContext db) : IProfileStore
{
    public Task<Profile?> FindByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
