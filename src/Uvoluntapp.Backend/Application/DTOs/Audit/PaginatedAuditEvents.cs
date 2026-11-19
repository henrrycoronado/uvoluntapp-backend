using Uvoluntapp.Backend.Application.DTOs.Common;

namespace Uvoluntapp.Backend.Application.DTOs.Audit;

public sealed record PaginatedAuditEvents(
    IReadOnlyList<AuditEventResponse> Items,
    PaginationMeta Meta);
