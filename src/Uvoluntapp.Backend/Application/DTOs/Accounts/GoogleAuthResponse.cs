using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

/// <summary>
/// Una cuenta externa sin solicitud de aprobación devuelve <see cref="RequiresApprovalData"/>
/// y los nombres de Google para precargar el formulario de datos.
/// </summary>
public sealed record GoogleAuthResponse(
    string AccountStatus,
    bool IsNewAccount,
    TokenPair? Tokens,
    string Email = "",
    bool RequiresApprovalData = false,
    string? FirstName = null,
    string? LastName = null);
