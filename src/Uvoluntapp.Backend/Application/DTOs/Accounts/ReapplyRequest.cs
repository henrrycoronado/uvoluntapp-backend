using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

/// <summary>Nueva solicitud de una cuenta externa rechazada; la contraseña prueba que es la dueña.</summary>
public sealed record ReapplyRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [JsonPropertyName("phone")]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("relationWithPastoral")]
    public string RelationWithPastoral { get; init; } = string.Empty;
}
