using Uvoluntapp.Backend.Application.DTOs.Administration;

namespace Uvoluntapp.Backend.Application.DTOs.Profiles;

public sealed record ProfileDetailResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? InstitutionalEmail,
    DateTime? InstitutionalEmailVerifiedAt,
    string AccountStatus,
    IReadOnlyList<RoleAssignmentResponse> Roles,
    DateTime CreatedAt,
    DateTime? VerifiedAt = null);
