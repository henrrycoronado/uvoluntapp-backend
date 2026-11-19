namespace Uvoluntapp.Backend.Application.DTOs.Administration;

public sealed record UserSummaryResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    DateTime CreatedAt,
    DateTime? VerifiedAt = null);
