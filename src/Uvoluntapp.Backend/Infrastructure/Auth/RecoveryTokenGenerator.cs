using System.Security.Cryptography;
using System.Text;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;

namespace Uvoluntapp.Backend.Infrastructure.Auth;

public sealed class RecoveryTokenGenerator : IRecoveryTokenGenerator
{
    public (string RawToken, string TokenHash) Create()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (raw, Hash(raw));
    }

    public string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
