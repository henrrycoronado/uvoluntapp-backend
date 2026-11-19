using System.Text.Json.Serialization;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public sealed record VerifyOtpResponse
{
    [JsonPropertyName("accountStatus")]
    public string AccountStatus { get; init; } = string.Empty;

    [JsonPropertyName("requiresApprovalData")]
    public bool RequiresApprovalData { get; init; }

    [JsonPropertyName("tokens")]
    public TokenPair? Tokens { get; init; }
}
