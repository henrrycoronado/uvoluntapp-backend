namespace Uvoluntapp.Backend.Application.DTOs.Profiles;

public sealed record ProfileResponse(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string? Phone);
