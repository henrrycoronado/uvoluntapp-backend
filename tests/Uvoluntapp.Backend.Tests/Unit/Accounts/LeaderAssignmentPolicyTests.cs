using System.Collections.Generic;
using System;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Policies;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public sealed class LeaderAssignmentPolicyTests
{
    private static readonly DateTime _now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid _programA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid _programB = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    private static RoleAssignment Leader(Guid programId) =>
        RoleAssignment.Assign(Guid.NewGuid(), AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, programId, Guid.NewGuid());

    private static IReadOnlyList<RoleAssignment> ActorLeadingA => [Leader(_programA)];

    [Fact]
    public void ProgramLeaderCanAssignOperationalLeaderInProgramItLeads()
    {
        Assert.True(LeaderAssignmentPolicy.CanAssign(ActorLeadingA, AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, _programA, _now));
    }

    [Fact]
    public void ProgramLeaderCannotAssignInAnotherProgram()
    {
        Assert.False(LeaderAssignmentPolicy.CanAssign(ActorLeadingA, AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, _programB, _now));
    }

    [Fact]
    public void ProgramLeaderCannotAssignAnotherProgramLeader()
    {
        Assert.False(LeaderAssignmentPolicy.CanAssign(ActorLeadingA, AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, _programA, _now));
    }

    [Fact]
    public void ProgramLeaderCannotAssignActivityScopedRoles()
    {
        Assert.False(LeaderAssignmentPolicy.CanAssign(ActorLeadingA, AssignedRole.OPERATIONAL_LEADER, RoleScope.ACTIVITY, _programA, _now));
    }

    [Fact]
    public void ProgramLeaderWithoutProgramCannotAssignAnything()
    {
        var unassigned = RoleAssignment.Assign(Guid.NewGuid(), AssignedRole.PROGRAM_LEADER, RoleScope.GLOBAL, null, Guid.NewGuid());
        Assert.False(LeaderAssignmentPolicy.CanAssign([unassigned], AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, _programA, _now));
    }

    [Fact]
    public void ProgramLeaderCanRevokeOperationalLeaderOfItsProgram()
    {
        var target = RoleAssignment.Assign(Guid.NewGuid(), AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, _programA, Guid.NewGuid());
        Assert.True(LeaderAssignmentPolicy.CanRevoke(ActorLeadingA, target, _now));
    }

    [Fact]
    public void ProgramLeaderCannotRevokeAnotherProgramLeader()
    {
        var target = Leader(_programA);
        Assert.False(LeaderAssignmentPolicy.CanRevoke(ActorLeadingA, target, _now));
    }

    [Fact]
    public void ProgramLeaderCannotRevokeOperationalLeaderOfAnotherProgram()
    {
        var target = RoleAssignment.Assign(Guid.NewGuid(), AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, _programB, Guid.NewGuid());
        Assert.False(LeaderAssignmentPolicy.CanRevoke(ActorLeadingA, target, _now));
    }

    [Fact]
    public void RevokedLeadershipNoLongerGrantsRights()
    {
        var revoked = Leader(_programA);
        revoked.Revoke(_now.AddMinutes(-1));
        Assert.False(LeaderAssignmentPolicy.CanAssign([revoked], AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, _programA, _now));
    }
}
