using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.Interfaces.Profiles;

public interface IProfileStore
{
    Task<Profile?> FindByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
