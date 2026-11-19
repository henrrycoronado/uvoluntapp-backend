using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Infrastructure.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public sealed class SessionTokenTests
{
    [Fact]
    public void IssuanceBindsTheSignedTokenToTheAccountAndSessionAndStoresOnlyTheRefreshHash()
    {
        var config = Configuration();
        var tokens = new SessionTokens(config, TimeProvider.System);
        var session = new AuthSession { UserId = Guid.NewGuid() };
        var now = DateTimeOffset.UtcNow;
        var result = tokens.Issue(session);
        var parameters = SessionTokens.ValidationParameters(config);
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }
            .ValidateToken(result.AccessToken, parameters, out var jwt);
        Assert.Equal(session.UserId.ToString(), principal.FindFirst("sub")!.Value);
        Assert.Equal(session.Id.ToString(), principal.FindFirst("sid")!.Value);
        Assert.DoesNotContain(principal.Claims, c => c.Type.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(jwt.ValidTo, result.ExpiresAt.UtcDateTime);
        Assert.InRange(result.ExpiresAt, now.AddMinutes(3).AddSeconds(-1), DateTimeOffset.UtcNow.AddMinutes(3));
        Assert.InRange(session.ExpiresAt, now.UtcDateTime.AddDays(2), DateTime.UtcNow.AddDays(2));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.RefreshToken))), session.RefreshTokenHash);
        var rotated = tokens.Issue(session);
        Assert.NotEqual(result.RefreshToken, rotated.RefreshToken);
        Assert.NotEqual(result.AccessToken, rotated.AccessToken);
        Assert.NotEqual(SessionTokens.Hash(result.RefreshToken), session.RefreshTokenHash);
    }

    [Fact]
    public void AccessTokenRejectsAnotherAudienceAndAnExpiredLifetime()
    {
        var config = Configuration();
        var result = new SessionTokens(config, TimeProvider.System).Issue(new AuthSession { UserId = Guid.NewGuid() });
        var validation = SessionTokens.ValidationParameters(config);
        validation.ValidAudience = "other";
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => new JwtSecurityTokenHandler().ValidateToken(result.AccessToken, validation, out _));
        validation.ValidAudience = config["JWT_AUDIENCE"];
        var expired = new SessionTokens(config, new PastClock()).Issue(new AuthSession { UserId = Guid.NewGuid() });
        Assert.Throws<SecurityTokenExpiredException>(() => new JwtSecurityTokenHandler().ValidateToken(expired.AccessToken, validation, out _));
    }

    [Fact]
    public void SigningRequiresAConfiguredKeyOfAtLeast256Bits()
    {
        var config = Configuration();
        config["JWT_SECRET"] = string.Empty;
        Assert.Throws<InvalidOperationException>(() => SessionTokens.ValidationParameters(config));
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["JWT_SECRET"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
        ["JWT_ISSUER"] = "unit-issuer",
        ["JWT_AUDIENCE"] = "unit-audience",
        ["JWT_EXPIRY_MINUTES"] = "3",
        ["JWT_REFRESH_DAYS"] = "2"
    }).Build();

    private sealed class PastClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddHours(-1);
    }
}
