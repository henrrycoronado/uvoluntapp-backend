using System.Security.Cryptography;
using Uvoluntapp.Backend.Application.Interfaces.Common;

namespace Uvoluntapp.Backend.Infrastructure.Auth;

public sealed class OneTimeCodeGenerator : IOneTimeCodeGenerator
{
    public string Create() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
