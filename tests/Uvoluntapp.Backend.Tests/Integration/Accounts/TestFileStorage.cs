using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Uvoluntapp.Backend.Application.Interfaces.Common;

namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

public sealed class TestFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();
    public Task PutAsync(string key, byte[] data, CancellationToken ct)
    {
        _files[key] = data;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        _files.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<string> ReadUrlAsync(string key) => Task.FromResult(_files.ContainsKey(key) ? "https://private.example.test/" + key : throw new System.InvalidOperationException());
}
