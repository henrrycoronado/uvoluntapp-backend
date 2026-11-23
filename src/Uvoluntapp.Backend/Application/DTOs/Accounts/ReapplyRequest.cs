using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

/// <summary>Nueva solicitud de una cuenta externa rechazada; la contraseña prueba que es la dueña.</summary>
public sealed record ReapplyRequest
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

    [Required]
    [MaxLength(FieldLimits.PersonName)]
    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(FieldLimits.PersonName)]
    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [MaxLength(FieldLimits.Phone)]
    [JsonPropertyName("phone")]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [MaxLength(FieldLimits.Relationship)]
    [JsonPropertyName("relationWithPastoral")]
    public string RelationWithPastoral { get; init; } = string.Empty;
}
