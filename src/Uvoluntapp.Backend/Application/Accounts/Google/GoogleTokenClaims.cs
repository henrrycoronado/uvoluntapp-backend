namespace Uvoluntapp.Backend.Application.Accounts.Google;

public sealed record GoogleTokenClaims(
    string Subject,
    string Email,
    bool EmailVerified,
    string Audience,
    string? HostedDomain);
