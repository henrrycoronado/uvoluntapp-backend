using Uvoluntapp.Backend.Application.DTOs.Common;

namespace Uvoluntapp.Backend.Application.Interfaces.Common;

public interface IImageProcessor
{
    ProcessedImage Decode(Stream source, string contentType);
}
