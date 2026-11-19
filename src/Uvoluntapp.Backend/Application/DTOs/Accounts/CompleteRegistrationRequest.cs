using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record CompleteRegistrationRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("phone")]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("relationWithPastoral")]
    public string RelationWithPastoral { get; init; } = string.Empty;
}
