using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Uvoluntapp.Backend.Presentation.Common;

internal static class CurrentUser
{
    public static Guid Id(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Falta el identificador de la sesión.");
}
