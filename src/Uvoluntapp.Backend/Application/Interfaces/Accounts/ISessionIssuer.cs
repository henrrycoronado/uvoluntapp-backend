using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

/// <summary>Abre una sesión (JWT y refresh token guardado como hash) para una cuenta ya autenticada.</summary>
public interface ISessionIssuer
{
    Task<TokenPair> IssueAsync(Guid userId, CancellationToken cancellationToken = default);
}
