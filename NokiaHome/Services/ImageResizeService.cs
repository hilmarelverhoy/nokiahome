using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace NokiaHome.Services;

public class ImageResizeService : IImageResizeService
{
    private const int QvgaWidth = 320;
    private const int QvgaHeight = 240;

    public async Task<(byte[] Data, string ContentType)> ResizeToQvgaAsync(Stream input, string fileName)
    {
        return await ResizeToQvgaAsync(input, fileName, 75);
    }

    public async Task<(byte[] Data, string ContentType)> ResizeToQvgaAsync(Stream input, string fileName, int quality)
    {
        using var image = await Image.LoadAsync(input);

        var (newWidth, newHeight) = CalculateDimensions(image.Width, image.Height, QvgaWidth, QvgaHeight);

        image.Mutate(x => x.Resize(newWidth, newHeight));

        using var output = new MemoryStream();
        var encoder = new JpegEncoder { Quality = quality };
        await image.SaveAsJpegAsync(output, encoder);

        return (output.ToArray(), "image/jpeg");
    }

    private static (int Width, int Height) CalculateDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        var ratioX = (double)maxWidth / originalWidth;
        var ratioY = (double)maxHeight / originalHeight;
        var ratio = Math.Min(ratioX, ratioY);

        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }
}