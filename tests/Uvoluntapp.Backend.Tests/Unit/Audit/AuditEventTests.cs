using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Uvoluntapp.Backend.Domain;
using Uvoluntapp.Backend.Domain.Audit;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Audit;

public class AuditEventTests
{
    [Fact]
    public void CreateStoresActorActionResourceAndUtcTimestamp()
    {
        var actorId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var localTime = new DateTime(2026, 11, 19, 12, 30, 0, DateTimeKind.Local);

        var auditEvent = AuditEvent.Create(
            actorId,
            AuditActions.RoleAssigned,
            AuditResourceTypes.RoleAssignment,
            resourceId,
            new Dictionary<string, object?> { ["role"] = "ADMIN" },
            localTime);

        Assert.Equal(actorId, auditEvent.ActorId);
        Assert.Equal(AuditActions.RoleAssigned, auditEvent.Action);
        Assert.Equal(AuditResourceTypes.RoleAssignment, auditEvent.ResourceType);
        Assert.Equal(resourceId, auditEvent.ResourceId);
        Assert.Equal(DateTimeKind.Utc, auditEvent.CreatedAt.Kind);
        Assert.Equal(localTime.ToUniversalTime(), auditEvent.CreatedAt);
    }

    [Fact]
    public void CreateTreatsUnspecifiedTimeAsUtc()
    {
        var moment = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Unspecified);

        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(),
            AuditActions.AccountApproved,
            AuditResourceTypes.AccountApprovalRequest,
            Guid.NewGuid(),
            occurredAt: moment);

        Assert.Equal(DateTimeKind.Utc, auditEvent.CreatedAt.Kind);
        Assert.Equal(moment.Ticks, auditEvent.CreatedAt.Ticks);
    }

    [Fact]
    public void CreateSystemHasNoActor()
    {
        var auditEvent = AuditEvent.CreateSystem(
            "ACTIVITY_STARTED",
            AuditResourceTypes.Activity,
            Guid.NewGuid());

        Assert.Null(auditEvent.ActorId);
    }

    [Fact]
    public void CreateRejectsEmptyActor()
    {
        Assert.Throws<ArgumentException>(() => AuditEvent.Create(
            Guid.Empty,
            AuditActions.RoleAssigned,
            AuditResourceTypes.RoleAssignment,
            Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsBlankAction(string action)
    {
        Assert.Throws<ArgumentException>(() => AuditEvent.Create(
            Guid.NewGuid(),
            action,
            AuditResourceTypes.RoleAssignment,
            Guid.NewGuid()));
    }

    [Fact]
    public void CreateRejectsBlankResourceTypeAndEmptyResourceId()
    {
        Assert.Throws<ArgumentException>(() => AuditEvent.Create(
            Guid.NewGuid(),
            AuditActions.RoleAssigned,
            "  ",
            Guid.NewGuid()));

        Assert.Throws<ArgumentException>(() => AuditEvent.Create(
            Guid.NewGuid(),
            AuditActions.RoleAssigned,
            AuditResourceTypes.RoleAssignment,
            Guid.Empty));
    }

    [Fact]
    public void CreateNeverStoresPasswordsOrTokens()
    {
        var details = new Dictionary<string, object?>
        {
            ["userId"] = Guid.NewGuid(),
            ["password"] = "Secreta123!",
            ["refreshToken"] = "token-en-claro",
            ["otpHash"] = "hash",
            ["nested"] = new Dictionary<string, object?>
            {
                ["access_token"] = "abc",
                ["safe"] = "visible",
            },
        };

        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(),
            AuditActions.AccountStatusChanged,
            AuditResourceTypes.User,
            Guid.NewGuid(),
            details);

        var data = JsonNode.Parse(auditEvent.Data)!.AsObject();

        Assert.False(data.ContainsKey("password"));
        Assert.False(data.ContainsKey("refreshToken"));
        Assert.False(data.ContainsKey("otpHash"));
        Assert.False(data["nested"]!.AsObject().ContainsKey("access_token"));
        Assert.Equal("visible", data["nested"]!["safe"]!.GetValue<string>());
    }

    [Fact]
    public void CreateWithoutDetailsStoresEmptyObject()
    {
        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(),
            AuditActions.RoleRevoked,
            AuditResourceTypes.RoleAssignment,
            Guid.NewGuid());

        Assert.Equal("{}", auditEvent.Data);
    }
}
