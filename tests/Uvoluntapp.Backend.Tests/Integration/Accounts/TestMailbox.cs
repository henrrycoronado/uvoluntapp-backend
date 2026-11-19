using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;

namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

public sealed class TestMailbox : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _codes = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _messages = new(StringComparer.OrdinalIgnoreCase);

    public string Code(string email) => _codes[email];

    public string Message(string email) => _messages[email];

    public Task SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken)
    {
        _codes[recipient] = Regex.Match(message, @"\b[0-9]{6}\b").Value;
        _messages[recipient] = message;
        return Task.CompletedTask;
    }
}
