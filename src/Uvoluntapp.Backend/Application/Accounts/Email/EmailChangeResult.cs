namespace Uvoluntapp.Backend.Application.Accounts.Email;

public sealed record EmailChangeResult(
    Guid UserId,
    string Email,
    DateTime EmailVerifiedAt,
    IReadOnlyList<Guid> SessionIdsToRevoke);
