using System;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Suspension;

public class AuthSessionTests
{
    private static readonly DateTime _now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SessionIsActiveWhileNotRevokedAndNotExpired()
    {
        var session = new AuthSession { ExpiresAt = _now.AddMinutes(5) };

        Assert.True(session.IsActive(_now));
    }

    [Fact]
    public void ExpiredSessionIsNotActive()
    {
        var session = new AuthSession { ExpiresAt = _now };

        Assert.False(session.IsActive(_now));
    }

    [Fact]
    public void RevokedSessionIsNotActive()
    {
        var session = new AuthSession
        {
            ExpiresAt = _now.AddMinutes(5),
            RevokedAt = _now.AddMinutes(-1),
        };

        Assert.False(session.IsActive(_now));
    }

    [Fact]
    public void RevokeKeepsTheSessionHistoryAndTheFirstRevocationTime()
    {
        var session = new AuthSession { ExpiresAt = _now.AddMinutes(5) };
        var createdAt = session.CreatedAt;
        var expiresAt = session.ExpiresAt;

        session.Revoke(_now);
        session.Revoke(_now.AddMinutes(1));

        Assert.Equal(_now, session.RevokedAt);
        Assert.Equal(createdAt, session.CreatedAt);
        Assert.Equal(expiresAt, session.ExpiresAt);
        Assert.False(session.IsActive(_now));
    }
}
