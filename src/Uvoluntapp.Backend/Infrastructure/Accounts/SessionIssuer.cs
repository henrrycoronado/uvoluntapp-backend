using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Application.Accounts.Contracts;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Accounts;

public sealed class SessionIssuer(AppDbContext db, SessionTokens tokens) : ISessionIssuer
{
    public async Task<TokenPair> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var issued = tokens.Issue(db.AuthSessions.Add(new AuthSession { UserId = userId }).Entity);
        await db.SaveChangesAsync(cancellationToken);
        return new TokenPair { AccessToken = issued.AccessToken, RefreshToken = issued.RefreshToken, ExpiresAt = issued.ExpiresAt };
    }
}
