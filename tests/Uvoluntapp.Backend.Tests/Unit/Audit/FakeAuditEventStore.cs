using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uvoluntapp.Backend.Application.Audit;
using Uvoluntapp.Backend.Domain;

namespace Uvoluntapp.Backend.Tests.Unit.Audit;

internal sealed class FakeAuditEventStore : IAuditEventStore
{
    public List<AuditEvent> Added { get; } = [];

    public AuditEventQueryResult Result { get; set; } = new([], 0);

    public AuditEventFilter? LastFilter { get; private set; }

    public int QueryCalls { get; private set; }

    public Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        Added.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<AuditEventQueryResult> QueryAsync(
        AuditEventFilter filter,
        CancellationToken cancellationToken = default)
    {
        QueryCalls++;
        LastFilter = filter;
        return Task.FromResult(Result);
    }
}
