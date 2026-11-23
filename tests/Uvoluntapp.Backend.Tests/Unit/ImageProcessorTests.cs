using System.IO;
using SkiaSharp;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;
using Uvoluntapp.Backend.Infrastructure.Storage;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit;

public sealed class ImageProcessorTests
{
    [Fact]
    public void RejectsTextDisguisedAsImage()
    {
        using var stream = new MemoryStream("test"u8.ToArray());
        Assert.Throws<DomainRuleViolationException>(() => new ImageProcessor().Decode(stream, "image/png"));
    }

    [Fact]
    public void RegeneratesThumbnailAndValidatesDeclaredType()
    {
        using var source = new SKBitmap(640, 400);
        using var encoded = source.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(encoded.ToArray());
        var result = new ImageProcessor().Decode(stream, "image/png");
        using var thumb = SKBitmap.Decode(result.Thumbnail);
        Assert.Equal(320, thumb.Width);
        Assert.Equal(200, thumb.Height);
        using var wrongMime = new MemoryStream(encoded.ToArray());
        Assert.Throws<DomainRuleViolationException>(() => new ImageProcessor().Decode(wrongMime, "image/jpeg"));
    }
}
