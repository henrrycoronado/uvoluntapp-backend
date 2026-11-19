using Uvoluntapp.Backend.Domain.Entities.Audit;

namespace Uvoluntapp.Backend.Application.DTOs.Audit;

public sealed record AuditEventQueryResult(IReadOnlyList<AuditEvent> Items, int TotalCount);
