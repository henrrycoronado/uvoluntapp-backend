using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IAccountSessions
{
    Task<TokenResponse> LoginAsync(LoginRequest request, string ip, CancellationToken cancellationToken);

    // Crea la primera contraseña de una cuenta de Google con el código enviado al iniciar sesión.
    Task<SetPasswordResponse> SetPasswordAsync(SetPasswordRequest request, string ip, CancellationToken cancellationToken);

    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, Guid sessionId, string refreshToken, CancellationToken cancellationToken);
}
