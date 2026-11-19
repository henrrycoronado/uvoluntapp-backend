using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

/// <summary>Primera contraseña de una cuenta creada con Google; el código prueba el buzón.</summary>
public sealed record SetPasswordRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{6}$")]
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
