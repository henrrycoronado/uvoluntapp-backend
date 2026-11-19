using System;
using Uvoluntapp.Backend.Application.Authorization;
using Uvoluntapp.Backend.Domain.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Authorization;

public class RoleAssignmentAuthorizationTests
{
    private readonly ContextAuthorizationService _authorization = new();
    private readonly Guid _programA = Guid.NewGuid();
    private readonly Guid _programB = Guid.NewGuid();

    [Fact]
    public void GlobalAssignmentRejectsScopeId()
    {
        Assert.Throws<ArgumentException>(() => RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.ADMIN, RoleScope.GLOBAL, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void ProgramAndActivityAssignmentsRequireScopeId()
    {
        Assert.Throws<ArgumentException>(() => RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, null, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.OPERATIONAL_LEADER, RoleScope.ACTIVITY, null, Guid.NewGuid()));
    }

    [Fact]
    public void AdminAssignmentCanBeGlobal()
    {
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.ADMIN, RoleScope.GLOBAL, null, Guid.NewGuid());

        Assert.Null(assignment.ScopeId);
        Assert.True(assignment.IsActiveAt(DateTime.UtcNow));
    }

    [Fact]
    public void ProgramLeaderInProgramADoesNotAccessProgramB()
    {
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, _programA, Guid.NewGuid());

        var result = _authorization.Authorize(new AuthorizationRequest(
            true,
            AccountStatus.ACTIVE,
            [assignment],
            AssignedRole.PROGRAM_LEADER,
            RoleScope.PROGRAM,
            _programB,
            null,
            true));

        Assert.Equal(AuthorizationDecision.SCOPE_NOT_COVERED, result);
    }

    [Fact]
    public void ProgramLeaderCoversActivityBelongingToAssignedProgram()
    {
        var activityId = Guid.NewGuid();
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, _programA, Guid.NewGuid());

        var result = _authorization.Authorize(new AuthorizationRequest(
            true,
            AccountStatus.ACTIVE,
            [assignment],
            AssignedRole.PROGRAM_LEADER,
            RoleScope.ACTIVITY,
            activityId,
            _programA,
            true));

        Assert.Equal(AuthorizationDecision.ALLOWED, result);
    }

    [Fact]
    public void ActivityAssignmentDoesNotCoverAnotherActivity()
    {
        var activityA = Guid.NewGuid();
        var activityB = Guid.NewGuid();
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.OPERATIONAL_LEADER, RoleScope.ACTIVITY, activityA, Guid.NewGuid());

        var result = _authorization.Authorize(new AuthorizationRequest(
            true,
            AccountStatus.ACTIVE,
            [assignment],
            AssignedRole.OPERATIONAL_LEADER,
            RoleScope.ACTIVITY,
            activityB,
            _programA,
            true));

        Assert.Equal(AuthorizationDecision.SCOPE_NOT_COVERED, result);
    }

    [Fact]
    public void RevokingAssignmentLeavesHistory()
    {
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.ADMIN, RoleScope.GLOBAL, null, Guid.NewGuid());
        var revokedAt = DateTime.UtcNow;

        assignment.Revoke(revokedAt);

        Assert.Equal(revokedAt, assignment.RevokedAt);
        Assert.False(assignment.IsActiveAt(revokedAt));
    }

    [Fact]
    public void ExpiredAssignmentDoesNotAuthorize()
    {
        var now = DateTime.UtcNow;
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.ADMIN, RoleScope.GLOBAL, null, Guid.NewGuid(),
            validUntil: now.AddMinutes(-1));

        var result = _authorization.Authorize(new AuthorizationRequest(
            true,
            AccountStatus.ACTIVE,
            [assignment],
            AssignedRole.ADMIN,
            RoleScope.GLOBAL,
            null,
            null,
            true,
            now));

        Assert.Equal(AuthorizationDecision.ASSIGNMENT_NOT_ACTIVE, result);
    }

    [Theory]
    [InlineData(AccountStatus.PENDING_APPROVAL)]
    [InlineData(AccountStatus.SUSPENDED)]
    [InlineData(AccountStatus.REJECTED)]
    public void NonActiveAccountDoesNotAuthorize(AccountStatus status)
    {
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.ADMIN, RoleScope.GLOBAL, null, Guid.NewGuid());

        var result = _authorization.Authorize(new AuthorizationRequest(
            true,
            status,
            [assignment],
            AssignedRole.ADMIN,
            RoleScope.GLOBAL,
            null,
            null,
            true));

        Assert.Equal(AuthorizationDecision.ACCOUNT_NOT_ACTIVE, result);
    }

    [Fact]
    public void ResourceStateMustAllowOperation()
    {
        var assignment = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.ADMIN, RoleScope.GLOBAL, null, Guid.NewGuid());

        var result = _authorization.Authorize(new AuthorizationRequest(
            true,
            AccountStatus.ACTIVE,
            [assignment],
            AssignedRole.ADMIN,
            RoleScope.GLOBAL,
            null,
            null,
            false));

        Assert.Equal(AuthorizationDecision.RESOURCE_STATE_NOT_ALLOWED, result);
    }

    [Fact]
    public void MultipleRolesCanBeHeldWithoutCrossResourcePermissions()
    {
        var otherProgram = Guid.NewGuid();
        var programLeader = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, _programA, Guid.NewGuid());
        var operationalLeader = RoleAssignment.Assign(
            Guid.NewGuid(), AssignedRole.OPERATIONAL_LEADER, RoleScope.ACTIVITY, Guid.NewGuid(), Guid.NewGuid());

        var result = _authorization.Authorize(new AuthorizationRequest(
            true,
            AccountStatus.ACTIVE,
            [programLeader, operationalLeader],
            AssignedRole.PROGRAM_LEADER,
            RoleScope.PROGRAM,
            otherProgram,
            null,
            true));

        Assert.Equal(AuthorizationDecision.SCOPE_NOT_COVERED, result);
    }
}
