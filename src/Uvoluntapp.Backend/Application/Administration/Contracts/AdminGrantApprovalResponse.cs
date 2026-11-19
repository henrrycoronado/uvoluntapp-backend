namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record AdminGrantApprovalResponse(Guid AdminId, DateTime ApprovedAt);
