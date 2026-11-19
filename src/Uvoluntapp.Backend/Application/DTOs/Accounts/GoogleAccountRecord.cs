using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record GoogleAccountRecord(
    Guid UserId,
    string Email,
    bool EmailVerified,
    AccountStatus Status);
