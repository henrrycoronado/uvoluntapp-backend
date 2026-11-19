using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.DTOs.Administration;

/// <summary>Un ADMIN reemplaza el correo de acceso de quien ya no puede usar el suyo.</summary>
public sealed record ChangeUserEmailRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [JsonPropertyName("newEmail")]
    public string NewEmail { get; init; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
