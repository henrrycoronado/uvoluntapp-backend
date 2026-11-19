namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed record GoogleAccountLinkDecision(
    AccountStatus Status,
    bool IsNewAccount,
    bool LinkExisting,
    bool InstitutionalEmailVerified);
