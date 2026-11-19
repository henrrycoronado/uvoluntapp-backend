using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
