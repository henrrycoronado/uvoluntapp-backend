using System.Net;
using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;
using Uvoluntapp.Backend.Application.Accounts;

namespace Uvoluntapp.Backend.Infrastructure.Accounts;

public sealed class AccountRateLimiter(IConnectionMultiplexer redis, IConfiguration config) : Uvoluntapp.Backend.Application.Accounts.Recovery.IAccountRateLimiter
{
    // Both dimensions share one atomic sliding window decision and the Redis server clock.
    private const string Script = """
        local time = redis.call('TIME')
        local now = time[1] * 1000 + math.floor(time[2] / 1000)
        for _, key in ipairs(KEYS) do
            redis.call('ZREMRANGEBYSCORE', key, '-inf', now - ARGV[1])
            if redis.call('ZCARD', key) >= tonumber(ARGV[2]) then return 0 end
        end
        for _, key in ipairs(KEYS) do
            redis.call('ZADD', key, now, ARGV[3])
            redis.call('PEXPIRE', key, ARGV[1])
        end
        return 1
        """;

    public Task CheckAsync(string operation, string accountKey, string ipAddress, CancellationToken cancellationToken) =>
        CheckAsync(operation, accountKey, ipAddress);

    public async Task CheckAsync(string operation, string normalizedEmail, string ip)
    {
        if (!config.GetValue("AUTH_RATE_LIMIT_ENABLED", true))
        {
            return;
        }

        if (IPAddress.TryParse(ip, out var address))
        {
            ip = address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
        }

        var prefix = $"uvoluntapp:auth:{{{operation}}}:";
        var emailHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));
        var result = await redis.GetDatabase().ScriptEvaluateAsync(
            Script,
            [prefix + "email:" + emailHash, prefix + "ip:" + ip],
            [config.GetValue("AUTH_RATE_WINDOW_SECONDS", 60) * 1000L, config.GetValue("AUTH_RATE_LIMIT", 5), Guid.NewGuid().ToString("N")]);
        if ((int)result == 0)
        {
            throw new AccountException(AccountError.TooManyAttempts);
        }
    }
}
