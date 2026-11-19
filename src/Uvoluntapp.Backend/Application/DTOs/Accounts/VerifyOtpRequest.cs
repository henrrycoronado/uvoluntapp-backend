using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record VerifyOtpRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "El código debe tener exactamente 6 dígitos.")]
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
}
