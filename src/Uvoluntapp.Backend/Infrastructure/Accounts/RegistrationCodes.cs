using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;
using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Infrastructure.Accounts;

// M1-B verifies a freshly constructed challenge. A random nonce and a server secret reproduce
// the code across nodes without persisting it. The account stamp changes atomically on activation.
public sealed class RegistrationCodes(IConnectionMultiplexer redis, IConfiguration config, TimeProvider clock)
{
    private const string Issue = """
        redis.call('HSET', KEYS[1], 'nonce', ARGV[1], 'issued', ARGV[2], 'stamp', ARGV[3], 'attempts', 0)
        return redis.call('PEXPIRE', KEYS[1], ARGV[4])
        """;

    private const string Attempt = """
        local count = redis.call('HINCRBY', KEYS[1], 'attempts', 1)
        if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1], 'NX') end
        return {count, unpack(redis.call('HMGET', KEYS[1], 'nonce', 'issued', 'stamp'))}
        """;

    public async Task<string?> IssueAsync(string normalizedEmail, string? stamp)
    {
        var db = redis.GetDatabase();
        var key = Key(normalizedEmail);
        if (stamp is null)
        {
            await db.KeyDeleteAsync(key);
            return null;
        }

        var issued = clock.GetUtcNow();
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var code = DeriveCode(nonce);
        var challenge = OneTimeCode.Create(code, issued);
        await db.ScriptEvaluateAsync(Issue, [key], [nonce, issued.ToUnixTimeMilliseconds(), stamp, (long)(challenge.ExpiresAt - issued).TotalMilliseconds]);
        return code;
    }

    public async Task VerifyAsync(string normalizedEmail, string? stamp, string code)
    {
        // A nonexistent or consumed challenge has the same attempt budget and error body.
        var reply = await redis.GetDatabase().ScriptEvaluateAsync(Attempt, [Key(normalizedEmail)], [(long)TimeSpan.FromMinutes(10).TotalMilliseconds]);
        var state = (RedisResult[])reply!;
        if ((long)state[0] > 5)
        {
            throw new AccountException(AccountError.TooManyAttempts);
        }

        if (state[1].IsNull || stamp is null || (string?)state[3] != stamp)
        {
            throw new AccountException(AccountError.InvalidCode);
        }

        var challenge = OneTimeCode.Create(DeriveCode(state[1].ToString()), DateTimeOffset.FromUnixTimeMilliseconds((long)state[2]));
        if (!challenge.Verify(code, clock.GetUtcNow()).IsValid)
        {
            throw new AccountException(AccountError.InvalidCode);
        }
    }

    private static RedisKey Key(string email) =>
        "uvoluntapp:auth:otp:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email)));

    private string DeriveCode(string nonce)
    {
        var key = Encoding.UTF8.GetBytes(config["JWT_SECRET"] ?? throw new InvalidOperationException("Falta JWT_SECRET."));
        var digest = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes("registration-otp.v1:" + nonce));
        return (BinaryPrimitives.ReadUInt64BigEndian(digest) % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }
}
