using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IGoogleTokenValidator
{
    Task<GoogleTokenClaims?> ValidateAsync(
        string idToken,
        IReadOnlyCollection<string> allowedAudiences,
        CancellationToken cancellationToken = default);
}
