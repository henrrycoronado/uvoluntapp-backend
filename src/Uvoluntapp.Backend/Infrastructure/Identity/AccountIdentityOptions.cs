using Microsoft.AspNetCore.Identity;

namespace Uvoluntapp.Backend.Infrastructure.Identity;

public static class AccountIdentityOptions
{
    public static void Configure(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = string.Empty;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    }
}
