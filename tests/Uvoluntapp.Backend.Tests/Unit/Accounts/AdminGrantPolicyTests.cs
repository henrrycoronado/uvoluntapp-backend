using System.Collections.Generic;
using System;
using Uvoluntapp.Backend.Domain.Policies;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public sealed class AdminGrantPolicyTests
{
    private static readonly Guid _beneficiary = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _adminA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid _adminB = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    private static IReadOnlySet<Guid> Active(params Guid[] ids) => new HashSet<Guid>(ids);

    [Fact]
    public void TwoDistinctActiveAdminsCanApprove()
    {
        AdminGrantPolicy.EnsureCanApprove(_beneficiary, _adminA, _adminB, Active(_adminA, _adminB));
    }

    [Fact]
    public void SameApproverTwiceIsRejected()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            AdminGrantPolicy.EnsureCanApprove(_beneficiary, _adminA, _adminA, Active(_adminA)));
    }

    [Fact]
    public void BeneficiaryCannotApprove()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            AdminGrantPolicy.EnsureCanApprove(_beneficiary, _beneficiary, _adminB, Active(_beneficiary, _adminB)));
    }

    [Fact]
    public void InactiveApproverIsRejected()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            AdminGrantPolicy.EnsureCanApprove(_beneficiary, _adminA, _adminB, Active(_adminA)));
    }

    [Fact]
    public void RemovingAdminIsRejectedWhenOnlyTwoRemain()
    {
        Assert.Throws<DomainRuleViolationException>(() => AdminGrantPolicy.EnsureCanRemoveAdmin(2));
    }

    [Fact]
    public void RemovingAdminIsAllowedWhenThreeAreActive()
    {
        AdminGrantPolicy.EnsureCanRemoveAdmin(3);
    }
}
