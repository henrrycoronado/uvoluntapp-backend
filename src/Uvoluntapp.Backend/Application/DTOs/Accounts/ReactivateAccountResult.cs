using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record ReactivateAccountResult(
    Guid UserId,
    AccountStatus Status,
    DateTime ReactivatedAt);
