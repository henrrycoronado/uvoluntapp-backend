namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed record GoogleIdentity(
    string Subject,
    string Email,
    bool EmailVerified,
    string? HostedDomain);
