using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record SuspendAccountResult(
    Guid UserId,
    AccountStatus Status,
    DateTime SuspendedAt,
    string Reason,
    IReadOnlyList<Guid> SessionIdsToRevoke);
