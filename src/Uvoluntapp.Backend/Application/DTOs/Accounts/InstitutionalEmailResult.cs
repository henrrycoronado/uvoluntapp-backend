namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record InstitutionalEmailResult(
    Guid UserId,
    string InstitutionalEmail,
    DateTime InstitutionalEmailVerifiedAt);
