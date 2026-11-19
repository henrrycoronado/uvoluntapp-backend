using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record GoogleAuthRequest
{
    public GoogleAuthRequest()
    {
    }

    public GoogleAuthRequest(string idToken) => IdToken = idToken;

    [Required]
    public string IdToken { get; init; } = string.Empty;
}
