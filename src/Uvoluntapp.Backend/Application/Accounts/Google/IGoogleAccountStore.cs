using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Google;

public interface IGoogleAccountStore
{
    Task<GoogleAccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<GoogleAccountRecord?> FindBySubjectAsync(string subject, CancellationToken cancellationToken);

    Task<GoogleAccountRecord> CreateAsync(
        GoogleIdentity identity,
        AccountStatus status,
        bool institutionalEmailVerified,
        CancellationToken cancellationToken);

    Task LinkAsync(
        Guid userId,
        GoogleIdentity identity,
        CancellationToken cancellationToken);
}
