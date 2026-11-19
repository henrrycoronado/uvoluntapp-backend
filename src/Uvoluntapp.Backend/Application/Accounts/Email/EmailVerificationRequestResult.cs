using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Email;

public sealed record EmailVerificationRequestResult(
    Guid UserId,
    string Email,
    EmailVerificationPurpose Purpose,
    DateTimeOffset ExpiresAt);
