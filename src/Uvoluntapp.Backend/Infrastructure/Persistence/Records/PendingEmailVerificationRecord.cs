using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Records;

public sealed class PendingEmailVerificationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public EmailVerificationPurpose Purpose { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public int Attempts { get; set; }

    public bool IsUsed { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
}
