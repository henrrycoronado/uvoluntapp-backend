using System;
using Uvoluntapp.Backend.Domain.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Email;

public class EmailAddressTests
{
    [Theory]
    [InlineData(" Ana.Perez@Example.COM ", "ana.perez@example.com")]
    [InlineData("ana.perez@ucb.edu.bo", "ana.perez@ucb.edu.bo")]
    [InlineData("ANA.PEREZ@UCB.EDU.BO", "ana.perez@ucb.edu.bo")]
    public void ValidAddressesAreNormalized(string input, string expected)
    {
        var address = EmailAddress.Create(input);

        Assert.Equal(expected, address.Value);
    }

    [Theory]
    [InlineData("ana.perez@ucb.edu.bo")]
    [InlineData("ANA.PEREZ@UCB.EDU.BO")]
    public void InstitutionalAddressesAreDetected(string email)
    {
        Assert.True(EmailAddress.Create(email).IsInstitutional);
    }

    [Theory]
    [InlineData("ana.perez@gmail.com")]
    [InlineData("ana.perez@ucb.edu.bo.evil.com")]
    [InlineData("ana.perez@fakeucb.edu.bo")]
    [InlineData("ana.perez@sub.ucb.edu.bo")]
    public void ExternalAddressesAreNotInstitutional(string email)
    {
        Assert.False(EmailAddress.Create(email).IsInstitutional);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana.perez")]
    [InlineData("@ucb.edu.bo")]
    [InlineData("ana@ucb@edu.bo")]
    [InlineData("ana perez@ucb.edu.bo")]
    public void InvalidAddressesAreRejected(string? email)
    {
        Assert.False(EmailAddress.TryCreate(email, out _));
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(email));
    }
}
