using System.Text.Json.Serialization;

using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record VerifyOtpResponse
{
    [JsonPropertyName("accountStatus")]
    public string AccountStatus { get; init; } = string.Empty;

    [JsonPropertyName("requiresApprovalData")]
    public bool RequiresApprovalData { get; init; }

    [JsonPropertyName("tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenPair? Tokens { get; init; }
}
