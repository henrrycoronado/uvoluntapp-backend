using SkiaSharp;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Infrastructure.Storage;

public sealed class ImageProcessor : IImageProcessor
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public ProcessedImage Decode(Stream source, string contentType)
    {
        if (source.Length is 0 or > MaxBytes)
        {
            throw Invalid();
        }

        using var codec = SKCodec.Create(source);
        if (codec is null)
        {
            throw Invalid();
        }

        var mime = codec.EncodedFormat switch { SKEncodedImageFormat.Png => "image/png", SKEncodedImageFormat.Jpeg => "image/jpeg", SKEncodedImageFormat.Webp => "image/webp", _ => null };
        var info = codec.Info;
        if (mime == null || mime != contentType || info.Width is < 1 or > 8192 || info.Height is < 1 or > 8192 || (long)info.Width * info.Height > 16000000 || codec.FrameCount > 1)
        {
            throw Invalid();
        }

        using var bitmap = new SKBitmap(info.Width, info.Height);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            throw Invalid();
        }

        return new ProcessedImage(Encode(bitmap, 1600), Encode(bitmap, 320));
    }

    private static byte[] Encode(SKBitmap original, int limit)
    {
        var scale = Math.Min(1d, (double)limit / Math.Max(original.Width, original.Height));
        using var resized = new SKBitmap(Math.Max(1, (int)(original.Width * scale)), Math.Max(1, (int)(original.Height * scale)));
        if (!original.ScalePixels(resized, new SKSamplingOptions(SKFilterMode.Linear)))
        {
            throw Invalid();
        }

        using var encoded = resized.Encode(SKEncodedImageFormat.Webp, 82);
        return encoded?.ToArray() ?? throw Invalid();
    }

    private static DomainRuleViolationException Invalid() => new("Se requiere una imagen JPEG, PNG o WebP válida de hasta 5 MiB y 16 megapíxeles.");
}
