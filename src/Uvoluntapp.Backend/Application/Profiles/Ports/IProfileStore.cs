using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Profiles.Ports;

public interface IProfileStore
{
    Task<Profile?> FindByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
