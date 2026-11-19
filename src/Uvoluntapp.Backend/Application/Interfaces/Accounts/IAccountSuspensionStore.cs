using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IAccountSuspensionStore
{
    Task<AccountStatus?> FindStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    // Devuelve las sesiones no revocadas del usuario. La capa de aplicación descarta las expiradas.
    Task<IReadOnlyList<AuthSession>> ListSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
