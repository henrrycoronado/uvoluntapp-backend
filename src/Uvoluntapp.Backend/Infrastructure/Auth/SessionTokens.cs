using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Infrastructure.Auth;

public sealed class SessionTokens(IConfiguration config, TimeProvider clock)
{
    public static TokenValidationParameters ValidationParameters(IConfiguration config)
    {
        var secret = Encoding.UTF8.GetBytes(config["JWT_SECRET"] ?? string.Empty);
        if (secret.Length < 32)
        {
            throw new InvalidOperationException("JWT_SECRET requiere al menos 32 bytes.");
        }

        return new TokenValidationParameters
        {
            ValidIssuer = config["JWT_ISSUER"] ?? throw new InvalidOperationException("Falta JWT_ISSUER."),
            ValidAudience = config["JWT_AUDIENCE"] ?? throw new InvalidOperationException("Falta JWT_AUDIENCE."),
            IssuerSigningKey = new SymmetricSecurityKey(secret),
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public TokenResponse Issue(AuthSession session)
    {
        var now = clock.GetUtcNow();
        var expires = DateTimeOffset.FromUnixTimeSeconds(now.AddMinutes(config.GetValue("JWT_EXPIRY_MINUTES", 60)).ToUnixTimeSeconds());
        var validation = ValidationParameters(config);
        var token = new JwtSecurityToken(
            validation.ValidIssuer,
            validation.ValidAudience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, session.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sid, session.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            now.UtcDateTime,
            expires.UtcDateTime,
            new SigningCredentials(validation.IssuerSigningKey, SecurityAlgorithms.HmacSha256));
        var refresh = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        session.RefreshTokenHash = Hash(refresh);
        session.ExpiresAt = now.AddDays(config.GetValue("JWT_REFRESH_DAYS", 15)).UtcDateTime;
        return new TokenResponse { AccessToken = new JwtSecurityTokenHandler().WriteToken(token), RefreshToken = refresh, ExpiresAt = expires };
    }
}
