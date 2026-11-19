using System.Text.Json.Serialization;

using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record SetPasswordResponse
{
    [JsonPropertyName("accountStatus")]
    public string AccountStatus { get; init; } = string.Empty;

    // Solo una cuenta activa abre sesión al crear su contraseña.
    [JsonPropertyName("tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenResponse? Tokens { get; init; }
}
