using System.Security.Cryptography;
using System.Text;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed class OneTimeCode
{
    private const int CodeLength = 6;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan _validity = TimeSpan.FromMinutes(10);

    private OneTimeCode(string codeHash, DateTimeOffset expiresAt)
    {
        CodeHash = codeHash;
        ExpiresAt = expiresAt;
    }

    public string CodeHash { get; }

    public DateTimeOffset ExpiresAt { get; }

    public int Attempts { get; private set; }

    public bool IsUsed { get; private set; }

    public static OneTimeCode Create(string code, DateTimeOffset createdAt)
    {
        ValidateCode(code);
        return new OneTimeCode(Hash(code), createdAt.ToUniversalTime().Add(_validity));
    }

    // Recupera un desafío ya emitido desde su almacén sin conocer el código en claro.
    public static OneTimeCode Restore(string codeHash, DateTimeOffset expiresAt, int attempts, bool isUsed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        if (attempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts));
        }

        return new OneTimeCode(codeHash, expiresAt)
        {
            Attempts = attempts,
            IsUsed = isUsed,
        };
    }

    public OneTimeCodeVerificationResult Verify(string code, DateTimeOffset now)
    {
        if (IsUsed)
        {
            return OneTimeCodeVerificationResult.Rejected(OneTimeCodeRejectionReason.ALREADY_USED);
        }

        if (now.ToUniversalTime() >= ExpiresAt)
        {
            return OneTimeCodeVerificationResult.Rejected(OneTimeCodeRejectionReason.EXPIRED);
        }

        if (Attempts >= MaxAttempts)
        {
            return OneTimeCodeVerificationResult.Rejected(OneTimeCodeRejectionReason.ATTEMPTS_EXHAUSTED);
        }

        var matches = CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(CodeHash),
            SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        if (!matches)
        {
            Attempts++;
            return OneTimeCodeVerificationResult.Rejected(OneTimeCodeRejectionReason.INVALID_CODE);
        }

        IsUsed = true;
        return OneTimeCodeVerificationResult.Accepted;
    }

    private static string Hash(string code) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static void ValidateCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (code.Length != CodeLength || code.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException("The one-time code must contain exactly six digits.", nameof(code));
        }
    }
}
