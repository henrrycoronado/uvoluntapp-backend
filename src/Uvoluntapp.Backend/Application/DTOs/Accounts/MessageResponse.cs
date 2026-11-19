using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record MessageResponse
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
