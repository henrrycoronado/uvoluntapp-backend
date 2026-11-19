namespace Uvoluntapp.Backend.Application.Profiles;

public sealed record ProfileResponse(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string? Phone);
