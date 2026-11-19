using System.Collections.Generic;
using System.Threading.Tasks;
using System;

using Xunit;

using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Services.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Suspension;

public class AccountSuspensionServiceTests
{
    private static readonly DateTime _now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _otherUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static IEnumerable<object[]> DeniedActorRoles()
    {
        yield return new object[] { Array.Empty<RoleAssignment>() };
        yield return new object[] { new[] { Role(_otherUserId, AssignedRole.ADMIN) } };
        yield return new object[] { new[] { Role(_adminId, AssignedRole.PROGRAM_LEADER) } };
        yield return new object[] { new[] { Role(_adminId, AssignedRole.ADMIN, RoleScope.PROGRAM) } };
        yield return new object[] { new[] { Role(_adminId, AssignedRole.ADMIN, revokedAt: _now.AddMinutes(-1)) } };
        yield return new object[] { new[] { Role(_adminId, AssignedRole.ADMIN, validFrom: _now.AddMinutes(1)) } };
        yield return new object[] { new[] { Role(_adminId, AssignedRole.ADMIN, validUntil: _now.AddMinutes(-1)) } };
    }

    [Fact]
    public async Task SuspendActiveAccountRevokesOnlyItsActiveSessions()
    {
        var store = StoreWithStatus(AccountStatus.ACTIVE);
        var active = Session(_userId, _now.AddHours(1));
        var expired = Session(_userId, _now.AddMinutes(-1));
        var alreadyRevoked = Session(_userId, _now.AddHours(1));
        alreadyRevoked.RevokedAt = _now.AddDays(-1);
        var otherUsers = Session(_otherUserId, _now.AddHours(1));
        store.Sessions.AddRange(new[] { active, expired, alreadyRevoked, otherUsers });

        var service = new AccountSuspensionService(store);

        var result = await service.SuspendAsync(
            _userId,
            "  Incumplimiento  ",
            _adminId,
            new[] { Role(_adminId, AssignedRole.ADMIN) },
            _now);

        Assert.Equal(_userId, result.UserId);
        Assert.Equal(AccountStatus.SUSPENDED, result.Status);
        Assert.Equal(_now, result.SuspendedAt);
        Assert.Equal("Incumplimiento", result.Reason);
        Assert.Equal(new[] { active.Id }, result.SessionIdsToRevoke);
        Assert.Equal(_now, active.RevokedAt);
        Assert.Null(expired.RevokedAt);
        Assert.Equal(_now.AddDays(-1), alreadyRevoked.RevokedAt);
        Assert.Null(otherUsers.RevokedAt);

        // La aplicación decide, pero no persiste: el estado sigue como estaba en el almacén.
        Assert.Equal(AccountStatus.ACTIVE, store.StatusByUser[_userId]);
    }

    [Theory]
    [MemberData(nameof(DeniedActorRoles))]
    public async Task SuspendDeniesActorsWithoutAnActiveGlobalAdminRole(RoleAssignment[] roles)
    {
        var store = StoreWithStatus(AccountStatus.ACTIVE);
        var service = new AccountSuspensionService(store);

        var exception = await Assert.ThrowsAsync<AccountSuspensionException>(
            () => service.SuspendAsync(_userId, "Incumplimiento", _adminId, roles, _now));

        Assert.Equal(AccountSuspensionError.Forbidden, exception.Error);
        Assert.Equal(0, store.FindStatusCalls);
    }

    [Theory]
    [InlineData(AccountStatus.PRE_REGISTERED)]
    [InlineData(AccountStatus.PENDING_VERIFICATION)]
    [InlineData(AccountStatus.PENDING_APPROVAL)]
    [InlineData(AccountStatus.REJECTED)]
    [InlineData(AccountStatus.SUSPENDED)]
    public async Task SuspendRejectsAccountsThatAreNotActive(AccountStatus status)
    {
        var store = StoreWithStatus(status);
        var service = new AccountSuspensionService(store);

        var exception = await Assert.ThrowsAsync<AccountSuspensionException>(
            () => service.SuspendAsync(
                _userId,
                "Incumplimiento",
                _adminId,
                new[] { Role(_adminId, AssignedRole.ADMIN) },
                _now));

        Assert.Equal(AccountSuspensionError.InvalidTransition, exception.Error);
        Assert.Equal(1, store.FindStatusCalls);
        Assert.Equal(0, store.ListSessionsCalls);
    }

    [Fact]
    public async Task SuspendRejectsAnUnknownAccount()
    {
        var store = new FakeAccountSuspensionStore();
        var service = new AccountSuspensionService(store);

        var exception = await Assert.ThrowsAsync<AccountSuspensionException>(
            () => service.SuspendAsync(
                _userId,
                "Incumplimiento",
                _adminId,
                new[] { Role(_adminId, AssignedRole.ADMIN) },
                _now));

        Assert.Equal(AccountSuspensionError.AccountNotFound, exception.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SuspendRequiresAReason(string reason)
    {
        var store = StoreWithStatus(AccountStatus.ACTIVE);
        var service = new AccountSuspensionService(store);

        var exception = await Assert.ThrowsAsync<AccountSuspensionException>(
            () => service.SuspendAsync(
                _userId,
                reason,
                _adminId,
                new[] { Role(_adminId, AssignedRole.ADMIN) },
                _now));

        Assert.Equal(AccountSuspensionError.InvalidReason, exception.Error);
        Assert.Equal(0, store.FindStatusCalls);
    }

    [Fact]
    public async Task ReactivateSuspendedAccountReturnsItToActive()
    {
        var store = StoreWithStatus(AccountStatus.SUSPENDED);
        var service = new AccountSuspensionService(store);

        var result = await service.ReactivateAsync(
            _userId,
            _adminId,
            new[] { Role(_adminId, AssignedRole.ADMIN) },
            _now);

        Assert.Equal(_userId, result.UserId);
        Assert.Equal(AccountStatus.ACTIVE, result.Status);
        Assert.Equal(_now, result.ReactivatedAt);
        Assert.Equal(0, store.ListSessionsCalls);
        Assert.Equal(AccountStatus.SUSPENDED, store.StatusByUser[_userId]);
    }

    [Theory]
    [InlineData(AccountStatus.PRE_REGISTERED)]
    [InlineData(AccountStatus.PENDING_VERIFICATION)]
    [InlineData(AccountStatus.PENDING_APPROVAL)]
    [InlineData(AccountStatus.ACTIVE)]
    [InlineData(AccountStatus.REJECTED)]
    public async Task ReactivateRejectsAccountsThatAreNotSuspended(AccountStatus status)
    {
        var store = StoreWithStatus(status);
        var service = new AccountSuspensionService(store);

        var exception = await Assert.ThrowsAsync<AccountSuspensionException>(
            () => service.ReactivateAsync(
                _userId,
                _adminId,
                new[] { Role(_adminId, AssignedRole.ADMIN) },
                _now));

        Assert.Equal(AccountSuspensionError.InvalidTransition, exception.Error);
    }

    [Fact]
    public async Task ReactivateRejectsAnUnknownAccount()
    {
        var store = new FakeAccountSuspensionStore();
        var service = new AccountSuspensionService(store);

        var exception = await Assert.ThrowsAsync<AccountSuspensionException>(
            () => service.ReactivateAsync(
                _userId,
                _adminId,
                new[] { Role(_adminId, AssignedRole.ADMIN) },
                _now));

        Assert.Equal(AccountSuspensionError.AccountNotFound, exception.Error);
    }

    [Fact]
    public async Task ReactivateDeniesNonAdminActors()
    {
        var store = StoreWithStatus(AccountStatus.SUSPENDED);
        var service = new AccountSuspensionService(store);

        var exception = await Assert.ThrowsAsync<AccountSuspensionException>(
            () => service.ReactivateAsync(_userId, _adminId, Array.Empty<RoleAssignment>(), _now));

        Assert.Equal(AccountSuspensionError.Forbidden, exception.Error);
        Assert.Equal(0, store.FindStatusCalls);
    }

    private static FakeAccountSuspensionStore StoreWithStatus(AccountStatus status)
    {
        var store = new FakeAccountSuspensionStore();
        store.StatusByUser[_userId] = status;
        return store;
    }

    private static AuthSession Session(Guid userId, DateTime expiresAt) =>
        new() { UserId = userId, ExpiresAt = expiresAt };

    private static RoleAssignment Role(
        Guid userId,
        AssignedRole role,
        RoleScope scope = RoleScope.GLOBAL,
        DateTime? validFrom = null,
        DateTime? validUntil = null,
        DateTime? revokedAt = null) =>
        new()
        {
            UserId = userId,
            Role = role,
            Scope = scope,
            GrantedBy = _adminId,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            RevokedAt = revokedAt,
        };
}
