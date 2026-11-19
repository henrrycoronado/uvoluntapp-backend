using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uvoluntapp.Backend.Application.Accounts;

namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

public sealed class TestMailbox : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _codes = new(StringComparer.OrdinalIgnoreCase);

    public string Code(string email) => _codes[email];

    public Task SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken)
    {
        _codes[recipient] = Regex.Match(message, @"\b[0-9]{6}\b").Value;
        return Task.CompletedTask;
    }
}
