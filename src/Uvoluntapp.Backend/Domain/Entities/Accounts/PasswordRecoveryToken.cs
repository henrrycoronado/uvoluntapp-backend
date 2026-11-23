namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed class PasswordRecoveryToken
{
    private PasswordRecoveryToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public string? NormalizedEmail { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? UsedAt { get; private set; }

    public static PasswordRecoveryToken Create(
        Guid userId,
        string tokenHash,
        DateTime issuedAt,
        string? email = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            NormalizedEmail = email?.Trim().ToUpperInvariant(),
            ExpiresAt = issuedAt.AddMinutes(30),
        };
    }

    public bool TryConsume(DateTime now)
    {
        if (UsedAt is not null || now >= ExpiresAt)
        {
            return false;
        }

        UsedAt = now;
        return true;
    }
}
