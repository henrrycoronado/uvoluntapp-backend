using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed record GoogleAccountLinkDecision(
    AccountStatus Status,
    bool IsNewAccount,
    bool LinkExisting,
    bool InstitutionalEmailVerified);
