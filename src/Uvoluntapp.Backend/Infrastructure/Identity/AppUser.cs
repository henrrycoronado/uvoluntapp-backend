using Microsoft.AspNetCore.Identity;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public AccountStatus Status { get; set; } = AccountStatus.PRE_REGISTERED;

    public string? InstitutionalEmail { get; set; }

    public DateTime? InstitutionalEmailVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SuspendedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
