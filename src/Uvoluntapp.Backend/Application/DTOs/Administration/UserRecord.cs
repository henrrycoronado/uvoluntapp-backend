using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Administration;

public sealed record UserRecord(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? InstitutionalEmail,
    DateTime? InstitutionalEmailVerifiedAt,
    AccountStatus Status,
    DateTime CreatedAt,
    DateTime? SuspendedAt);
