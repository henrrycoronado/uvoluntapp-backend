using Uvoluntapp.Backend.Application.Services.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class InstitutionalEmailPolicyTests
{
    [Theory]
    [InlineData("ana.perez@ucb.edu.bo")]
    [InlineData("ANA.PEREZ@UCB.EDU.BO")]
    public void InstitutionalDomainIsRecognized(string email)
    {
        Assert.True(InstitutionalEmailPolicy.IsInstitutional(email));
    }

    [Theory]
    [InlineData("ana.perez@gmail.com")]
    [InlineData("ana.perez@ucb.edu.bo.evil.com")]
    [InlineData("ana.perez@fakeucb.edu.bo")]
    [InlineData("ana.perez@sub.ucb.edu.bo")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void OtherDomainsAreExternal(string? email)
    {
        Assert.False(InstitutionalEmailPolicy.IsInstitutional(email));
    }
}
