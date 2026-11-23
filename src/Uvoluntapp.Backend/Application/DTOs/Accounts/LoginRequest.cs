using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(FieldLimits.Email)]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(FieldLimits.Password)]
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
