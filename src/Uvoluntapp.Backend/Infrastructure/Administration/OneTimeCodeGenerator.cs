using System.Security.Cryptography;
using Uvoluntapp.Backend.Application.Common;

namespace Uvoluntapp.Backend.Infrastructure.Administration;

public sealed class OneTimeCodeGenerator : IOneTimeCodeGenerator
{
    public string Create() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
