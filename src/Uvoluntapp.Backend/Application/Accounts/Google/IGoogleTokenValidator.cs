namespace Uvoluntapp.Backend.Application.Accounts.Google;

public interface IGoogleTokenValidator
{
    Task<GoogleTokenClaims?> ValidateAsync(
        string idToken,
        IReadOnlyCollection<string> allowedAudiences,
        CancellationToken cancellationToken = default);
}
