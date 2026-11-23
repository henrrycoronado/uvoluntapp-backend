using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IRecoveryAccountStore
{
    Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task SaveTokenAsync(PasswordRecoveryToken token, CancellationToken cancellationToken);

    Task<PasswordRecoveryToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken);

    // Reemplaza la contraseña, revoca todas las sesiones activas y audita el cambio.
    Task SetPasswordAsync(PasswordRecoveryToken claimed, string newPassword, DateTime utcNow, CancellationToken cancellationToken);
}
