namespace Uvoluntapp.Backend.Application.Accounts.Recovery;

public interface IAccountRateLimiter
{
    Task CheckAsync(string operation, string accountKey, string ipAddress, CancellationToken cancellationToken);
}
