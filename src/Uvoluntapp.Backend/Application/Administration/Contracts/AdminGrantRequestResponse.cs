namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record AdminGrantRequestResponse(
    Guid Id,
    Guid BeneficiaryId,
    Guid RequestedBy,
    string Status,
    IReadOnlyList<AdminGrantApprovalResponse> Approvals,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ResolvedAt);
