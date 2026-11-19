namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IAccountRateLimiter
{
    Task CheckAsync(string operation, string accountKey, string ipAddress, CancellationToken cancellationToken);
}
