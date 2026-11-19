using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public sealed record ResendOtpRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
