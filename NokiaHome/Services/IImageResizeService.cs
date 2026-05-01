using SixLabors.ImageSharp;

namespace NokiaHome.Services;

public interface IImageResizeService
{
    Task<(byte[] Data, string ContentType)> ResizeToQvgaAsync(Stream input, string fileName);
    Task<(byte[] Data, string ContentType)> ResizeToQvgaAsync(Stream input, string fileName, int quality);
}