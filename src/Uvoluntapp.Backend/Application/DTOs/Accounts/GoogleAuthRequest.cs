using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record GoogleAuthRequest
{
    public GoogleAuthRequest()
    {
    }

    public GoogleAuthRequest(string idToken) => IdToken = idToken;

    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(30)]
    public string? Phone { get; init; }

    [Required]
    public string IdToken { get; init; } = string.Empty;
}
