using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record RegisterRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;
}
