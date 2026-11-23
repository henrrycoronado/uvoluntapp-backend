using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class MediaStore(AppDbContext db) : IMediaStore
{
    public Task<MediaAsset?> FindAsync(ImageOwner owner, Guid id, CancellationToken ct) =>
        db.Set<MediaAsset>().SingleOrDefaultAsync(a => owner == ImageOwner.Profile ? a.UserId == id : owner == ImageOwner.Program ? a.ProgramId == id : a.ActivityId == id, ct);

    public async Task WithOwnerLockAsync(ImageOwner owner, Guid id, Func<Task> operation, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        FormattableString sql = owner switch
        {
            ImageOwner.Profile => $"SELECT id FROM users WHERE id = {id} FOR UPDATE",
            ImageOwner.Program => $"SELECT id FROM programs WHERE id = {id} FOR UPDATE",
            _ => $"SELECT id FROM activities WHERE id = {id} FOR UPDATE",
        };
        await db.Database.ExecuteSqlInterpolatedAsync(sql, ct);
        await operation();
        await tx.CommitAsync(ct);
    }

    public async Task SaveAsync(MediaAsset? add, CancellationToken ct)
    {
        if (add != null)
        {
            db.Add(add);
        }

        await db.SaveChangesAsync(ct);
    }
}
