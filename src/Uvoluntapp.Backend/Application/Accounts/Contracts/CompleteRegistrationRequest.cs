using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

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
