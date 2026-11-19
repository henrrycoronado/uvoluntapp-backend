using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Suspension;

public sealed record ReactivateAccountResult(
    Guid UserId,
    AccountStatus Status,
    DateTime ReactivatedAt);
