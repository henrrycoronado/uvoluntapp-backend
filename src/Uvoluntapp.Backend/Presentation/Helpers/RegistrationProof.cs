using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;

namespace Uvoluntapp.Backend.Presentation.Helpers;

public sealed class RegistrationProof(IDataProtectionProvider protection)
{
    private const string Cookie = "uvoluntapp.registration";
    private readonly ITimeLimitedDataProtector _protector = protection.CreateProtector("accounts.complete-registration.v1").ToTimeLimitedDataProtector();

    public void Issue(HttpResponse response, string email)
    {
        response.Cookies.Append(Cookie, _protector.Protect(email.Trim().ToUpperInvariant(), TimeSpan.FromMinutes(30)), Options());
    }

    public void Require(HttpRequest request, string email)
    {
        if (request.Cookies.TryGetValue(Cookie, out var proof))
        {
            try
            {
                if (_protector.Unprotect(proof) == email.Trim().ToUpperInvariant())
                {
                    return;
                }
            }
            catch (CryptographicException)
            {
                // Expired and invalid proofs have the same response.
            }
        }

        throw new AccountException(AccountError.RegistrationConflict);
    }

    public void Clear(HttpResponse response) => response.Cookies.Delete(Cookie, Options());

    private static CookieOptions Options() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth/complete-registration"
    };
}
