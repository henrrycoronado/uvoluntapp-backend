using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Ports;

public interface IAccountDirectory
{
    Task<AccountStatus?> FindStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserRecord?> FindAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<UserRecord>> SearchAsync(
        AccountStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SetStatusAsync(
        Guid userId,
        AccountStatus status,
        DateTime? suspendedAt,
        CancellationToken cancellationToken = default);

    Task RevokeSessionsAsync(
        IReadOnlyList<Guid> sessionIds,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);
}
