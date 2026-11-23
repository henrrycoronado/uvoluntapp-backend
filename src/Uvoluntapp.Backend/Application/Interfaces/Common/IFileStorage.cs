namespace Uvoluntapp.Backend.Application.Interfaces.Common;

public interface IFileStorage
{
    Task PutAsync(string key, byte[] data, CancellationToken ct);

    Task DeleteAsync(string key, CancellationToken ct);

    Task<string> ReadUrlAsync(string key);
}
