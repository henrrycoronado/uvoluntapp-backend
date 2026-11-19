using Uvoluntapp.Backend.Application.Administration.Contracts;

namespace Uvoluntapp.Backend.Application.Profiles.Contracts;

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
    DateTime CreatedAt);
