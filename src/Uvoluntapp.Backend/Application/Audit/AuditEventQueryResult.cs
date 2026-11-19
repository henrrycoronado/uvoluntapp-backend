using Uvoluntapp.Backend.Domain;

namespace Uvoluntapp.Backend.Application.Audit;

public sealed record AuditEventQueryResult(IReadOnlyList<AuditEvent> Items, int TotalCount);
