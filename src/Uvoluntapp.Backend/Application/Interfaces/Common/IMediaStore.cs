using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Common;

public interface IMediaStore
{
    Task<MediaAsset?> FindAsync(ImageOwner owner, Guid id, CancellationToken ct);

    Task WithOwnerLockAsync(ImageOwner owner, Guid id, Func<Task> operation, CancellationToken ct);

    Task SaveAsync(MediaAsset? add, CancellationToken ct);
}
