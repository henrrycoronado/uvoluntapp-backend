using Amazon.S3;
using Amazon.S3.Model;
using Uvoluntapp.Backend.Application.Interfaces.Common;

namespace Uvoluntapp.Backend.Infrastructure.Storage;

public sealed class R2FileStorage(IConfiguration config) : IFileStorage, IDisposable
{
    private readonly Lazy<AmazonS3Client> _client = new(() => new AmazonS3Client(config["S3_ACCESS_KEY"], config["S3_SECRET_KEY"], new AmazonS3Config { ServiceURL = config["S3_ENDPOINT_URL"], ForcePathStyle = true, AuthenticationRegion = "auto" }));

    public async Task PutAsync(string key, byte[] data, CancellationToken ct)
    {
        using var stream = new MemoryStream(data);
        await _client.Value.PutObjectAsync(new PutObjectRequest { BucketName = config["STORAGE_UPLOAD_BUCKET"], Key = key, ContentType = "image/webp", InputStream = stream, DisablePayloadSigning = true, DisableDefaultChecksumValidation = true }, ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct) => _client.Value.DeleteObjectAsync(config["STORAGE_UPLOAD_BUCKET"], key, ct);

    public Task<string> ReadUrlAsync(string key) => _client.Value.GetPreSignedURLAsync(new GetPreSignedUrlRequest { BucketName = config["STORAGE_UPLOAD_BUCKET"], Key = key, Expires = DateTime.UtcNow.AddMinutes(2) });

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }
    }
}
