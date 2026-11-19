using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Auth;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Presentation.Helpers;

public static class AccountBearer
{
    public static void Configure(JwtBearerOptions options, IConfiguration configuration)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = SessionTokens.ValidationParameters(configuration);
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ValidateSessionAsync,
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.Headers.WWWAuthenticate = "Bearer";
                return Results.Problem(statusCode: 401, title: "Se necesita una sesión válida.").ExecuteAsync(context.HttpContext);
            }
        };
    }

    private static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
            || !Guid.TryParse(context.Principal?.FindFirst(JwtRegisteredClaimNames.Sid)?.Value, out var sessionId))
        {
            context.Fail("Sesión inválida.");
            return;
        }

        var services = context.HttpContext.RequestServices;
        var db = services.GetRequiredService<AppDbContext>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var active = await (from session in db.AuthSessions
                            join user in db.Users on session.UserId equals user.Id
                            where session.Id == sessionId && user.Id == userId && user.EmailConfirmed && user.Status == AccountStatus.ACTIVE
                                && user.DeletedAt == null && session.RevokedAt == null && session.ExpiresAt > now && session.DeletedAt == null
                            select session.Id).AnyAsync(context.HttpContext.RequestAborted);
        if (!active)
        {
            context.Fail("Sesión inválida.");
        }
    }
}
