using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Uvoluntapp.Backend.Application.DTOs.Audit;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Audit;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Audit;

public class AuditQueryServiceTests
{
    private static readonly DateTime _now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task OnlyAdminWithActiveGlobalAssignmentCanQuery()
    {
        var store = new FakeAuditEventStore();
        var service = new AuditQueryService(store);
        var actorId = Guid.NewGuid();
        var roles = new[]
        {
            new RoleAssignment
            {
                UserId = actorId,
                Role = AssignedRole.PROGRAM_LEADER,
                Scope = RoleScope.GLOBAL,
            },
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ListAsync(new AuditEventFilter(), actorId, roles, _now));

        Assert.Equal(0, store.QueryCalls);
    }

    [Fact]
    public async Task AdminWithoutActiveGlobalAssignmentCannotQuery()
    {
        var store = new FakeAuditEventStore();
        var service = new AuditQueryService(store);
        var actorId = Guid.NewGuid();
        var roles = new[]
        {
            new RoleAssignment
            {
                UserId = actorId,
                Role = AssignedRole.ADMIN,
                Scope = RoleScope.PROGRAM,
                ScopeId = Guid.NewGuid(),
            },
            new RoleAssignment
            {
                UserId = actorId,
                Role = AssignedRole.ADMIN,
                Scope = RoleScope.GLOBAL,
                RevokedAt = _now.AddDays(-1),
            },
            new RoleAssignment
            {
                UserId = actorId,
                Role = AssignedRole.ADMIN,
                Scope = RoleScope.GLOBAL,
                ValidUntil = _now.AddDays(-1),
            },
            new RoleAssignment
            {
                UserId = actorId,
                Role = AssignedRole.ADMIN,
                Scope = RoleScope.GLOBAL,
                ValidFrom = _now.AddDays(1),
            },
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ListAsync(new AuditEventFilter(), actorId, roles, _now));

        Assert.Equal(0, store.QueryCalls);
    }

    [Fact]
    public async Task AdminGetsPaginatedEventsWithNormalizedFilters()
    {
        var store = new FakeAuditEventStore();
        var service = new AuditQueryService(store);
        var actorId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var auditEvent = AuditEvent.Create(
            actorId,
            AuditActions.RoleAssigned,
            AuditResourceTypes.RoleAssignment,
            resourceId,
            new Dictionary<string, object?> { ["role"] = "ADMIN" },
            _now);
        store.Result = new AuditEventQueryResult([auditEvent], 45);

        var result = await service.ListAsync(
            new AuditEventFilter(ActorId: actorId, Action: " ROLE_ASSIGNED ", Page: 2, PageSize: 20),
            actorId,
            [AdminRole(actorId)],
            _now);

        Assert.NotNull(store.LastFilter);
        Assert.Equal(actorId, store.LastFilter!.ActorId);
        Assert.Equal("ROLE_ASSIGNED", store.LastFilter.Action);
        Assert.Equal(2, store.LastFilter.Page);

        var item = Assert.Single(result.Items);
        Assert.Equal(auditEvent.Id, item.Id);
        Assert.Equal(actorId, item.ActorId);
        Assert.Equal(AuditActorType.USER, item.ActorType);
        Assert.Equal(auditEvent.Action, item.Action);
        Assert.Equal(resourceId, item.EntityId);
        Assert.Equal("ADMIN", item.Details!["role"]!.GetValue<string>());
        Assert.Equal(_now, item.CreatedAt);

        Assert.Equal(2, result.Meta.Page);
        Assert.Equal(20, result.Meta.PageSize);
        Assert.Equal(45, result.Meta.TotalCount);
        Assert.Equal(3, result.Meta.TotalPages);
    }

    [Fact]
    public async Task EmptyResultHasZeroPages()
    {
        var store = new FakeAuditEventStore();
        var service = new AuditQueryService(store);
        var actorId = Guid.NewGuid();

        var result = await service.ListAsync(
            new AuditEventFilter(),
            actorId,
            [AdminRole(actorId)],
            _now);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Meta.TotalPages);
    }

    [Fact]
    public async Task SystemEventsHaveNoActor()
    {
        var store = new FakeAuditEventStore();
        var service = new AuditQueryService(store);
        var actorId = Guid.NewGuid();
        var systemEvent = AuditEvent.CreateSystem(
            "ACTIVITY_STARTED",
            AuditResourceTypes.Activity,
            Guid.NewGuid());
        store.Result = new AuditEventQueryResult([systemEvent], 1);

        var result = await service.ListAsync(
            new AuditEventFilter(EntityType: "ACTIVITY"),
            actorId,
            [AdminRole(actorId)],
            _now);

        var item = Assert.Single(result.Items);
        Assert.Null(item.ActorId);
        Assert.Equal(AuditActorType.SYSTEM, item.ActorType);
        Assert.Equal("ACTIVITY", store.LastFilter!.EntityType);
    }

    [Fact]
    public async Task FilterValidationRunsAfterAuthorization()
    {
        var store = new FakeAuditEventStore();
        var service = new AuditQueryService(store);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ListAsync(new AuditEventFilter(Page: 0), Guid.NewGuid(), [], _now));

        Assert.Equal(0, store.QueryCalls);
    }

    private static RoleAssignment AdminRole(Guid actorId) =>
        new()
        {
            UserId = actorId,
            Role = AssignedRole.ADMIN,
            Scope = RoleScope.GLOBAL,
            ValidFrom = _now.AddDays(-1),
        };
}
