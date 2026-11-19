using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Presentation.Helpers;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class RegistrationProofTests
{
    [Theory]
    [InlineData("otro@example.org", false)]
    [InlineData("ANA@example.org", true)]
    public void ProofIsBoundToTheVerifiedAddress(string email, bool accepted)
    {
        var proof = new RegistrationProof(new EphemeralDataProtectionProvider());
        var http = new DefaultHttpContext();
        proof.Issue(http.Response, "ana@example.org");
        var cookie = SetCookieHeaderValue.Parse(http.Response.Headers.SetCookie.ToString());
        Assert.True(cookie.HttpOnly);
        Assert.True(cookie.Secure);
        Assert.Equal("/api/v1/auth/complete-registration", cookie.Path.Value);
        http.Request.Headers.Cookie = $"{cookie.Name}={cookie.Value}";

        if (accepted)
        {
            proof.Require(http.Request, email);
        }
        else
        {
            var error = Assert.Throws<AccountException>(() => proof.Require(http.Request, email));
            Assert.Equal(AccountError.RegistrationConflict, error.Error);
        }
    }
}
