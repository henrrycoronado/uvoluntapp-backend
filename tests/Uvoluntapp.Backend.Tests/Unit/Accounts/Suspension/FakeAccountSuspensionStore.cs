using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Suspension;

internal sealed class FakeAccountSuspensionStore : IAccountSuspensionStore
{
    public Dictionary<Guid, AccountStatus> StatusByUser { get; } = new();

    public List<AuthSession> Sessions { get; } = new();

    public int FindStatusCalls { get; private set; }

    public int ListSessionsCalls { get; private set; }

    public Task<AccountStatus?> FindStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        FindStatusCalls++;
        AccountStatus? status = StatusByUser.TryGetValue(userId, out var value) ? value : null;
        return Task.FromResult(status);
    }

    public Task<IReadOnlyList<AuthSession>> ListSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ListSessionsCalls++;
        IReadOnlyList<AuthSession> sessions = Sessions.Where(session => session.UserId == userId).ToList();
        return Task.FromResult(sessions);
    }
}
