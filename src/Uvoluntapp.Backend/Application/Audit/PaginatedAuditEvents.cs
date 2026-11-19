namespace Uvoluntapp.Backend.Application.Audit;

public sealed record PaginatedAuditEvents(
    IReadOnlyList<AuditEventResponse> Items,
    PaginationMeta Meta);
