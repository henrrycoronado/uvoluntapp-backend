using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Policies;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Accounts;

public sealed class AccountSuspensionService(IAccountSuspensionStore store)
{
    public async Task<SuspendAccountResult> SuspendAsync(
        Guid userId,
        string reason,
        Guid actorId,
        IEnumerable<RoleAssignment> actorRoles,
        DateTime? now = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);
        ArgumentNullException.ThrowIfNull(actorRoles);

        var utcNow = now ?? DateTime.UtcNow;
        EnsureAdmin(actorId, actorRoles, utcNow);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new AccountSuspensionException(
                AccountSuspensionError.InvalidReason,
                "El motivo de la suspensión es obligatorio.");
        }

        var status = await store.FindStatusAsync(userId, cancellationToken)
            ?? throw new AccountSuspensionException(
                AccountSuspensionError.AccountNotFound,
                "La cuenta no existe.");

        if (!AccountStatusTransitions.CanSuspend(status))
        {
            throw new AccountSuspensionException(
                AccountSuspensionError.InvalidTransition,
                $"No se puede suspender una cuenta en estado {status}.");
        }

        var nextStatus = AccountStatusTransitions.Suspend(status);
        var sessions = await store.ListSessionsAsync(userId, cancellationToken);
        var sessionIdsToRevoke = RevokeActiveSessions(userId, sessions, utcNow);

        // La persistencia del estado y de las sesiones queda en Infrastructure.
        return new SuspendAccountResult(userId, nextStatus, utcNow, reason.Trim(), sessionIdsToRevoke);
    }

    public async Task<ReactivateAccountResult> ReactivateAsync(
        Guid userId,
        Guid actorId,
        IEnumerable<RoleAssignment> actorRoles,
        DateTime? now = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actorRoles);

        var utcNow = now ?? DateTime.UtcNow;
        EnsureAdmin(actorId, actorRoles, utcNow);

        var status = await store.FindStatusAsync(userId, cancellationToken)
            ?? throw new AccountSuspensionException(
                AccountSuspensionError.AccountNotFound,
                "La cuenta no existe.");

        if (!AccountStatusTransitions.CanReactivate(status))
        {
            throw new AccountSuspensionException(
                AccountSuspensionError.InvalidTransition,
                $"No se puede reactivar una cuenta en estado {status}.");
        }

        var nextStatus = AccountStatusTransitions.Reactivate(status);

        return new ReactivateAccountResult(userId, nextStatus, utcNow);
    }

    private static IReadOnlyList<Guid> RevokeActiveSessions(
        Guid userId,
        IEnumerable<AuthSession> sessions,
        DateTime utcNow)
    {
        var revokedSessionIds = new List<Guid>();

        foreach (var session in sessions)
        {
            if (session.UserId != userId || !session.IsActive(utcNow))
            {
                continue;
            }

            session.Revoke(utcNow);
            revokedSessionIds.Add(session.Id);
        }

        return revokedSessionIds;
    }

    private static void EnsureAdmin(Guid actorId, IEnumerable<RoleAssignment> actorRoles, DateTime utcNow)
    {
        var isAdmin = actorId != Guid.Empty
            && actorRoles.Any(role =>
                role.UserId == actorId
                && role.Role == AssignedRole.ADMIN
                && role.Scope == RoleScope.GLOBAL
                && role.RevokedAt is null
                && (role.ValidFrom is null || role.ValidFrom <= utcNow)
                && (role.ValidUntil is null || role.ValidUntil >= utcNow));

        if (!isAdmin)
        {
            throw new AccountSuspensionException(
                AccountSuspensionError.Forbidden,
                "Solo un ADMIN global activo puede suspender o reactivar cuentas.");
        }
    }
}
