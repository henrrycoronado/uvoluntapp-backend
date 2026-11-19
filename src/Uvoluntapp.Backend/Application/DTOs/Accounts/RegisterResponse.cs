using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record RegisterResponse
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("requiresOtp")]
    public bool RequiresOtp { get; init; }
}
