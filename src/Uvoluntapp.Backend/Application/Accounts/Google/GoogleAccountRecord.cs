using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Google;

public sealed record GoogleAccountRecord(
    Guid UserId,
    string Email,
    bool EmailVerified,
    AccountStatus Status);
