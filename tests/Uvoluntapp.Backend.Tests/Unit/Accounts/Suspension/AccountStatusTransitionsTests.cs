using System;

using Xunit;

using Uvoluntapp.Backend.Domain.Policies;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;
namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Suspension;

public class AccountStatusTransitionsTests
{
    [Fact]
    public void ActiveAccountCanBeSuspended()
    {
        Assert.True(AccountStatusTransitions.CanSuspend(AccountStatus.ACTIVE));
        Assert.Equal(AccountStatus.SUSPENDED, AccountStatusTransitions.Suspend(AccountStatus.ACTIVE));
    }

    [Fact]
    public void SuspendedAccountCanBeReactivated()
    {
        Assert.True(AccountStatusTransitions.CanReactivate(AccountStatus.SUSPENDED));
        Assert.Equal(AccountStatus.ACTIVE, AccountStatusTransitions.Reactivate(AccountStatus.SUSPENDED));
    }

    [Theory]
    [InlineData(AccountStatus.PRE_REGISTERED)]
    [InlineData(AccountStatus.PENDING_VERIFICATION)]
    [InlineData(AccountStatus.PENDING_APPROVAL)]
    [InlineData(AccountStatus.REJECTED)]
    [InlineData(AccountStatus.SUSPENDED)]
    public void SuspendRejectsAnyOtherStatus(AccountStatus current)
    {
        Assert.False(AccountStatusTransitions.CanSuspend(current));

        var exception = Assert.Throws<InvalidAccountStatusTransitionException>(
            () => AccountStatusTransitions.Suspend(current));

        Assert.Equal(current, exception.From);
        Assert.Equal(AccountStatus.SUSPENDED, exception.To);
    }

    [Theory]
    [InlineData(AccountStatus.PRE_REGISTERED)]
    [InlineData(AccountStatus.PENDING_VERIFICATION)]
    [InlineData(AccountStatus.PENDING_APPROVAL)]
    [InlineData(AccountStatus.ACTIVE)]
    [InlineData(AccountStatus.REJECTED)]
    public void ReactivateRejectsAnyOtherStatus(AccountStatus current)
    {
        Assert.False(AccountStatusTransitions.CanReactivate(current));

        var exception = Assert.Throws<InvalidAccountStatusTransitionException>(
            () => AccountStatusTransitions.Reactivate(current));

        Assert.Equal(current, exception.From);
        Assert.Equal(AccountStatus.ACTIVE, exception.To);
    }
}
