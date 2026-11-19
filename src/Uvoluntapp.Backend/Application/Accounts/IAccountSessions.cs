using Uvoluntapp.Backend.Application.Accounts.Contracts;

namespace Uvoluntapp.Backend.Application.Accounts;

public interface IAccountSessions
{
    Task<TokenResponse> LoginAsync(LoginRequest request, string ip, CancellationToken cancellationToken);

    // Crea la primera contraseña de una cuenta de Google con el código enviado al iniciar sesión.
    Task<SetPasswordResponse> SetPasswordAsync(SetPasswordRequest request, string ip, CancellationToken cancellationToken);

    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, Guid sessionId, string refreshToken, CancellationToken cancellationToken);
}
