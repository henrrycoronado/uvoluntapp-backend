using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Interfaces.Profiles;
using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class ProfileStore(AppDbContext db) : IProfileStore
{
    public Task<Profile?> FindByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
