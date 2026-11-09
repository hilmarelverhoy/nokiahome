using System.Text;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;

namespace NokiaHome.Services
{
    /// <summary>Converts content to the device wire formats (CP437 text, packed 1-bit bitmaps).</summary>
    public static class PrintRenderer
    {
        public const int PrinterWidth = 384;

        static PrintRenderer() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        public static byte[] EncodeCp437(string text)
        {
            var enc = Encoding.GetEncoding(437, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
            return enc.GetBytes(text.Replace("\r\n", "\n").Replace('\r', '\n'));
        }

        /// <summary>Packs a bitmap MSB-first, 1 = black. Rows beyond maxBytes are truncated.</summary>
        public static byte[] Pack(Image<L8> img, int maxBytes)
        {
            var rowBytes = img.Width / 8;
            var rows = Math.Min(img.Height, maxBytes / rowBytes);
            var data = new byte[rows * rowBytes];
            for (var y = 0; y < rows; y++)
                for (var x = 0; x < img.Width; x++)
                    if (img[x, y].PackedValue < 128)
                        data[y * rowBytes + x / 8] |= (byte)(0x80 >> (x % 8));
            return data;
        }

        public static byte[] RenderQr(string text, int size, int maxBytes)
        {
            using var gen = new QRCodeGenerator();
            using var qr = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            var modules = qr.ModuleMatrix; // includes quiet zone
            var n = modules.Count;
            var scale = Math.Max(1, Math.Min(size, PrinterWidth) / n);
            var qrPx = n * scale;
            var offset = Math.Max(0, (PrinterWidth - qrPx) / 2);

            using var canvas = new Image<L8>(PrinterWidth, qrPx, new L8(255));
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    if (!modules[y][x]) continue;
                    for (var dy = 0; dy < scale; dy++)
                        for (var dx = 0; dx < scale; dx++)
                        {
                            var px = offset + x * scale + dx;
                            if (px < PrinterWidth) canvas[px, y * scale + dy] = new L8(0);
                        }
                }
            return Pack(canvas, maxBytes);
        }

        public static byte[] RenderImage(Stream input, int maxBytes)
        {
            using var img = Image.Load<Rgba32>(input);
            img.Mutate(c =>
            {
                c.BackgroundColor(Color.White);
                c.Resize(PrinterWidth, 0);
                c.Grayscale();
            });
            using var gray = img.CloneAs<L8>();
            gray.Mutate(c => c.BinaryDither(KnownDitherings.FloydSteinberg));
            return Pack(gray, maxBytes);
        }
    }
}
