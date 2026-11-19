using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record ResendOtpRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
