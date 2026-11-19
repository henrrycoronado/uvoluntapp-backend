using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed class AuthSession : Entity
{
    public Guid UserId { get; set; }

    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;

    // Revocar no borra la sesión: conserva su historial y solo fija la fecha.
    public void Revoke(DateTime utcNow)
    {
        RevokedAt ??= utcNow;
    }
}
