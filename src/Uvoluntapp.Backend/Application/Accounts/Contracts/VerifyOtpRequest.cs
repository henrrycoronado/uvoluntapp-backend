using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public sealed record VerifyOtpRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener exactamente 6 dígitos.")]
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
}
