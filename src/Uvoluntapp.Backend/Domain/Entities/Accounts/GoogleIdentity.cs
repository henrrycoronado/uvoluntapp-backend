namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed record GoogleIdentity(
    string Subject,
    string Email,
    bool EmailVerified,
    string? HostedDomain,
    string? GivenName = null,
    string? FamilyName = null);
