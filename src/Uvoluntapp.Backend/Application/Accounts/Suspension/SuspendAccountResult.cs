using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Suspension;

public sealed record SuspendAccountResult(
    Guid UserId,
    AccountStatus Status,
    DateTime SuspendedAt,
    string Reason,
    IReadOnlyList<Guid> SessionIdsToRevoke);
