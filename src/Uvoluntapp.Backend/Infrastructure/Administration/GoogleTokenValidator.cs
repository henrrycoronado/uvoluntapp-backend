using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Uvoluntapp.Backend.Application.Accounts.Google;

namespace Uvoluntapp.Backend.Infrastructure.Administration;

/// <summary>Valida el id_token con el endpoint tokeninfo de Google. La audiencia la comprueba la capa de aplicación.</summary>
public sealed class GoogleTokenValidator(HttpClient client) : IGoogleTokenValidator
{
    public async Task<GoogleTokenClaims?> ValidateAsync(
        string idToken,
        IReadOnlyCollection<string> allowedAudiences,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenInfo>(cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Sub) || string.IsNullOrWhiteSpace(payload.Email))
        {
            return null;
        }

        return new GoogleTokenClaims(
            payload.Sub,
            payload.Email,
            string.Equals(payload.EmailVerified, "true", StringComparison.OrdinalIgnoreCase),
            payload.Aud ?? string.Empty,
            payload.Hd,
            payload.GivenName,
            payload.FamilyName);
    }

    private sealed class TokenInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("email_verified")]
        public string? EmailVerified { get; init; }

        [JsonPropertyName("aud")]
        public string? Aud { get; init; }

        [JsonPropertyName("hd")]
        public string? Hd { get; init; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; init; }
    }
}
