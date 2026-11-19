namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record UserDetailResponse(
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
    DateTime? SuspendedAt);
