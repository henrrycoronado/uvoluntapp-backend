using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Auth;

public sealed class SessionIssuer(AppDbContext db, SessionTokens tokens) : ISessionIssuer
{
    public async Task<TokenPair> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var issued = tokens.Issue(db.AuthSessions.Add(new AuthSession { UserId = userId }).Entity);
        await db.SaveChangesAsync(cancellationToken);
        return new TokenPair { AccessToken = issued.AccessToken, RefreshToken = issued.RefreshToken, ExpiresAt = issued.ExpiresAt };
    }
}
