namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record EmailChangeResult(
    Guid UserId,
    string Email,
    DateTime EmailVerifiedAt,
    IReadOnlyList<Guid> SessionIdsToRevoke);
