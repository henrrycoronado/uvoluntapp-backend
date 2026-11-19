namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record UserSummaryResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    DateTime CreatedAt);
