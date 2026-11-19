using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Suspension;

public interface IAccountSuspensionStore
{
    Task<AccountStatus?> FindStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    // Devuelve las sesiones no revocadas del usuario. La capa de aplicación descarta las expiradas.
    Task<IReadOnlyList<AuthSession>> ListSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
