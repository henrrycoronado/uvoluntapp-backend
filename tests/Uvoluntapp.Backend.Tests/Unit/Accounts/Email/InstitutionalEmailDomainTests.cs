using Xunit;

using Uvoluntapp.Backend.Domain.Utils.Constants;
namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Email;

public class InstitutionalEmailDomainTests
{
    [Theory]
    [InlineData("ana.perez@ucb.edu.bo")]
    [InlineData("ANA.PEREZ@UCB.EDU.BO")]
    [InlineData(" ana.perez@ucb.edu.bo ")]
    public void UcbAddressesAreInstitutional(string email)
    {
        Assert.True(InstitutionalEmailDomain.IsInstitutional(email));
    }

    [Theory]
    [InlineData("ana.perez@gmail.com")]
    [InlineData("ana.perez@ucb.edu.bo.evil.com")]
    [InlineData("ana.perez@fakeucb.edu.bo")]
    [InlineData("ana.perez@sub.ucb.edu.bo")]
    [InlineData("@ucb.edu.bo")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void OtherAddressesAreNotInstitutional(string? email)
    {
        Assert.False(InstitutionalEmailDomain.IsInstitutional(email));
    }
}
