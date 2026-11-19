namespace Uvoluntapp.Backend.Application.Accounts.Email;

public sealed record InstitutionalEmailResult(
    Guid UserId,
    string InstitutionalEmail,
    DateTime InstitutionalEmailVerifiedAt);
