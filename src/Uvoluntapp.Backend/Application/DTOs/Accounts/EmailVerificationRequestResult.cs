using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record EmailVerificationRequestResult(
    Guid UserId,
    string Email,
    EmailVerificationPurpose Purpose,
    DateTimeOffset ExpiresAt);
