using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Recovery;

public interface IRecoveryAccountStore
{
    Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task SaveTokenAsync(PasswordRecoveryToken token, CancellationToken cancellationToken);

    Task<PasswordRecoveryToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken);

    Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
}
