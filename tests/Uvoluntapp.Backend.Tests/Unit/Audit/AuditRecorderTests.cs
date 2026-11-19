using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Audit;

public class AuditRecorderTests
{
    private static readonly DateTime _now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AssigningARoleEmitsAnAuditEvent()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);
        var actorId = Guid.NewGuid();
        var assignment = new RoleAssignment
        {
            UserId = Guid.NewGuid(),
            Role = AssignedRole.PROGRAM_LEADER,
            Scope = RoleScope.PROGRAM,
            ScopeId = Guid.NewGuid(),
            GrantedBy = actorId,
        };

        await recorder.RecordRoleAssignedAsync(actorId, assignment, _now);

        var auditEvent = Assert.Single(store.Added);
        Assert.Equal(AuditActions.RoleAssigned, auditEvent.Action);
        Assert.Equal(AuditResourceTypes.RoleAssignment, auditEvent.ResourceType);
        Assert.Equal(assignment.Id, auditEvent.ResourceId);
        Assert.Equal(actorId, auditEvent.ActorId);
        Assert.Equal(_now, auditEvent.CreatedAt);

        var details = JsonNode.Parse(auditEvent.Data)!;
        Assert.Equal(assignment.UserId, details["userId"]!.GetValue<Guid>());
        Assert.Equal("PROGRAM_LEADER", details["role"]!.GetValue<string>());
        Assert.Equal(assignment.ScopeId, details["scopeId"]!.GetValue<Guid>());
    }

    [Fact]
    public async Task RevokingARoleEmitsAnAuditEvent()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);
        var actorId = Guid.NewGuid();
        var assignment = new RoleAssignment
        {
            UserId = Guid.NewGuid(),
            Role = AssignedRole.ADMIN,
            RevokedAt = _now,
        };

        await recorder.RecordRoleRevokedAsync(actorId, assignment, _now);

        var auditEvent = Assert.Single(store.Added);
        Assert.Equal(AuditActions.RoleRevoked, auditEvent.Action);
        Assert.Equal(assignment.Id, auditEvent.ResourceId);
    }

    [Fact]
    public async Task ApprovingAnAccountEmitsAnAuditEvent()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);
        var actorId = Guid.NewGuid();
        var request = new AccountApprovalRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Pérez",
            Relationship = "Voluntaria",
        };

        await recorder.RecordAccountApprovedAsync(actorId, request, _now);

        var auditEvent = Assert.Single(store.Added);
        Assert.Equal(AuditActions.AccountApproved, auditEvent.Action);
        Assert.Equal(AuditResourceTypes.AccountApprovalRequest, auditEvent.ResourceType);
        Assert.Equal(request.Id, auditEvent.ResourceId);
        Assert.Equal("ACTIVE", JsonNode.Parse(auditEvent.Data)!["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task RejectingAnAccountEmitsAnAuditEventWithTheReason()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);
        var actorId = Guid.NewGuid();
        var request = new AccountApprovalRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = "Luis",
            LastName = "Gómez",
            Relationship = "Externo",
        };

        await recorder.RecordAccountRejectedAsync(actorId, request, "No acredita relación con Pastoral", _now);

        var auditEvent = Assert.Single(store.Added);
        Assert.Equal(AuditActions.AccountRejected, auditEvent.Action);

        var details = JsonNode.Parse(auditEvent.Data)!;
        Assert.Equal("REJECTED", details["status"]!.GetValue<string>());
        Assert.Equal("No acredita relación con Pastoral", details["reason"]!.GetValue<string>());
        Assert.Null(details["password"]);
        Assert.Null(details["token"]);
    }

    [Fact]
    public async Task ChangingAccountStatusEmitsAnAuditEvent()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await recorder.RecordAccountStatusChangedAsync(
            actorId,
            userId,
            AccountStatus.ACTIVE,
            AccountStatus.SUSPENDED,
            "Incumplimiento",
            _now);

        var auditEvent = Assert.Single(store.Added);
        Assert.Equal(AuditActions.AccountStatusChanged, auditEvent.Action);
        Assert.Equal(AuditResourceTypes.User, auditEvent.ResourceType);
        Assert.Equal(userId, auditEvent.ResourceId);

        var details = JsonNode.Parse(auditEvent.Data)!;
        Assert.Equal("ACTIVE", details["previousStatus"]!.GetValue<string>());
        Assert.Equal("SUSPENDED", details["newStatus"]!.GetValue<string>());
    }

    [Fact]
    public async Task ChangingStatusToTheSameValueIsRejected()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);

        await Assert.ThrowsAsync<ArgumentException>(() => recorder.RecordAccountStatusChangedAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AccountStatus.ACTIVE,
            AccountStatus.ACTIVE));

        Assert.Empty(store.Added);
    }

    [Fact]
    public async Task RejectingAnAccountRequiresAReason()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);
        var request = new AccountApprovalRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = "Luis",
            LastName = "Gómez",
            Relationship = "Externo",
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => recorder.RecordAccountRejectedAsync(Guid.NewGuid(), request, "  "));

        Assert.Empty(store.Added);
    }

    [Fact]
    public async Task SystemActionsAreRecordedWithoutActor()
    {
        var store = new FakeAuditEventStore();
        var recorder = new AuditRecorder(store);
        var activityId = Guid.NewGuid();

        await recorder.RecordSystemAsync(
            "ACTIVITY_STARTED",
            AuditResourceTypes.Activity,
            activityId,
            occurredAt: _now);

        var auditEvent = Assert.Single(store.Added);
        Assert.Null(auditEvent.ActorId);
        Assert.Equal(activityId, auditEvent.ResourceId);
    }
}
