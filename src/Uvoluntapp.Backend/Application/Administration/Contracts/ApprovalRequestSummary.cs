namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record ApprovalRequestSummary(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string RelationWithPastoral,
    string Status,
    string? RejectionReason,
    Guid? ReviewedBy,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
