using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uvoluntapp.Backend.Application.Accounts.Email;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Email;

internal sealed class FakeAccountEmailStore : IAccountEmailStore
{
    public Dictionary<Guid, AccountEmail> Accounts { get; } = new();

    public Dictionary<string, Guid> EmailOwners { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<AuthSession> Sessions { get; } = new();

    public Dictionary<(Guid UserId, EmailVerificationPurpose Purpose), PendingEmailVerification> Pending { get; } = new();

    public string ValidPassword { get; set; } = "Secreta123!";

    public int SaveCalls { get; private set; }

    public int SavePendingCalls { get; private set; }

    public int RemovePendingCalls { get; private set; }

    public int ListSessionsCalls { get; private set; }

    public Task<AccountEmail?> FindAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        AccountEmail? account = Accounts.TryGetValue(userId, out var value) ? value : null;
        return Task.FromResult(account);
    }

    public Task<bool> IsEmailInUseAsync(
        string email,
        Guid exceptUserId,
        CancellationToken cancellationToken = default)
    {
        var inUse = EmailOwners.TryGetValue(email, out var owner) && owner != exceptUserId;
        return Task.FromResult(inUse);
    }

    public Task<bool> VerifyPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(password == ValidPassword);

    public Task<IReadOnlyList<AuthSession>> ListSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ListSessionsCalls++;
        IReadOnlyList<AuthSession> sessions = Sessions
            .Where(session => session.UserId == userId)
            .ToList();
        return Task.FromResult(sessions);
    }

    public Task<PendingEmailVerification?> FindPendingAsync(
        Guid userId,
        EmailVerificationPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        PendingEmailVerification? pending = Pending.TryGetValue((userId, purpose), out var value)
            ? value
            : null;
        return Task.FromResult(pending);
    }

    public Task SaveAsync(AccountEmail account, CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        Accounts[account.UserId] = account;
        return Task.CompletedTask;
    }

    public Task SavePendingAsync(
        Guid userId,
        PendingEmailVerification pending,
        CancellationToken cancellationToken = default)
    {
        SavePendingCalls++;
        Pending[(userId, pending.Purpose)] = pending;
        return Task.CompletedTask;
    }

    public Task RemovePendingAsync(
        Guid userId,
        EmailVerificationPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        RemovePendingCalls++;
        Pending.Remove((userId, purpose));
        return Task.CompletedTask;
    }
}
