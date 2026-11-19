using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Ports;

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
