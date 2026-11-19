using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Email;

public interface IAccountEmailStore
{
    Task<AccountEmail?> FindAsync(Guid userId, CancellationToken cancellationToken = default);

    // Comprueba el correo frente a los correos de acceso e institucionales de otras cuentas.
    Task<bool> IsEmailInUseAsync(
        string email,
        Guid exceptUserId,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default);

    // Devuelve las sesiones de la cuenta. La capa de aplicación descarta las revocadas y expiradas.
    Task<IReadOnlyList<AuthSession>> ListSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PendingEmailVerification?> FindPendingAsync(
        Guid userId,
        EmailVerificationPurpose purpose,
        CancellationToken cancellationToken = default);

    Task SaveAsync(AccountEmail account, CancellationToken cancellationToken = default);

    Task SavePendingAsync(
        Guid userId,
        PendingEmailVerification pending,
        CancellationToken cancellationToken = default);

    Task RemovePendingAsync(
        Guid userId,
        EmailVerificationPurpose purpose,
        CancellationToken cancellationToken = default);
}
