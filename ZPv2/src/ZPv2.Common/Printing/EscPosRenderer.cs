using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using ZPv2.Common.Models;

namespace ZPv2.Common.Printing;

public sealed class EscPosRenderer
{
    private static readonly Encoding Enc = Encoding.GetEncoding(860);

    public byte[] Build(PrintJobRequest request)
    {
        var type = (request.Type ?? "RECEIPT").Trim().ToUpperInvariant();
        return type switch
        {
            "DRAWER" => BuildDrawerOnly(),
            "CUT" => BuildCutOnly(request.CutMode),
            "TEST" => BuildTextReceipt(request.Text, request.Cut, request.OpenDrawer, request.CutMode, request.FeedLines, "Teste ZPv2"),
            "FISCAL_PDF" => BuildFiscalPdf(request),
            _ => BuildTextReceipt(request.Text, request.Cut, request.OpenDrawer, request.CutMode, request.FeedLines, null),
        };
    }

    private byte[] BuildTextReceipt(string? text, bool cut, bool openDrawer, string? cutMode, int feedLines, string? header)
    {
        var bytes = new List<byte>(1024);
        bytes.AddRange(CmdInit());
        bytes.AddRange(CmdAlign(1));
        if (!string.IsNullOrWhiteSpace(header))
        {
            bytes.AddRange(CmdBold(true));
            AppendLine(bytes, header);
            bytes.AddRange(CmdBold(false));
        }
        bytes.AddRange(CmdAlign(0));
        AppendLine(bytes, "ZPv2");
        AppendLine(bytes, DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        AppendLine(bytes, "------------------------------------------");
        var content = string.IsNullOrWhiteSpace(text) ? "Documento emitido pelo POS." : text.Trim();
        foreach (var line in content.Split('\n'))
        {
            AppendLine(bytes, line.TrimEnd('\r'));
        }

        if (openDrawer)
        {
            bytes.AddRange(OpenDrawer());
        }

        bytes.AddRange(CmdFeed(Math.Clamp(feedLines <= 0 ? 6 : feedLines, 4, 6)));
        if (cut)
        {
            bytes.AddRange(Cut(cutMode));
        }
        return bytes.ToArray();
    }

    private byte[] BuildFiscalPdf(PrintJobRequest request)
    {
        var bytes = new List<byte>(8192);
        bytes.AddRange(CmdInit());
        bytes.AddRange(CmdAlign(1));

        var width = NormalizeWidth(request.WidthDots);
        var segment = Math.Clamp(request.SegmentHeight <= 0 ? 1200 : request.SegmentHeight, 420, 2200);
        var pages = request.Pages ?? new List<string>();
        var printed = 0;

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page)) continue;
            var chunks = BuildRasterChunksFromBase64(page, width, segment);
            foreach (var chunk in chunks)
            {
                bytes.AddRange(chunk);
                bytes.Add(0x0A);
                printed++;
            }
            bytes.Add(0x0A);
        }

        if (printed == 0)
        {
            AppendLine(bytes, "[ZPv2] Sem páginas renderizáveis no job fiscal.");
        }

        bytes.AddRange(CmdFeed(Math.Clamp(request.FeedLines <= 0 ? 6 : request.FeedLines, 4, 6)));
        if (request.OpenDrawer)
        {
            bytes.AddRange(OpenDrawer());
        }
        if (request.Cut)
        {
            bytes.AddRange(Cut(request.CutMode));
        }

        return bytes.ToArray();
    }

    private byte[] BuildDrawerOnly()
    {
        var bytes = new List<byte>(16);
        bytes.AddRange(CmdInit());
        bytes.AddRange(OpenDrawer());
        return bytes.ToArray();
    }

    private byte[] BuildCutOnly(string? cutMode)
    {
        var bytes = new List<byte>(16);
        bytes.AddRange(CmdInit());
        bytes.AddRange(Cut(cutMode));
        return bytes.ToArray();
    }

    private static int NormalizeWidth(int raw)
    {
        var allowed = new[] { 384, 512, 576, 640 };
        if (allowed.Contains(raw)) return raw;
        var nearest = 576;
        var delta = int.MaxValue;
        foreach (var v in allowed)
        {
            var d = Math.Abs(v - raw);
            if (d < delta)
            {
                delta = d;
                nearest = v;
            }
        }
        return nearest;
    }

    private static byte[]? DecodeBase64Image(string raw)
    {
        var value = raw.Trim();
        if (value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            var comma = value.IndexOf(',');
            if (comma >= 0 && comma + 1 < value.Length)
            {
                value = value[(comma + 1)..];
            }
        }
        if (value.Length == 0) return null;
        try
        {
            return Convert.FromBase64String(value);
        }
        catch
        {
            return null;
        }
    }

    private static List<byte[]> BuildRasterChunksFromBase64(string rawBase64, int maxWidth, int segmentHeight)
    {
        var list = new List<byte[]>();
        var sourceBytes = DecodeBase64Image(rawBase64);
        if (sourceBytes is null || sourceBytes.Length == 0) return list;

        using var stream = new MemoryStream(sourceBytes);
        using var source = new Bitmap(stream);
        if (source.Width <= 0 || source.Height <= 0) return list;

        var width = Math.Max(120, Math.Min(640, maxWidth));
        var scale = (decimal)width / Math.Max(1, source.Width);
        if (scale <= 0) scale = 1m;
        var scaledHeight = Math.Max(1, (int)Math.Round(source.Height * (double)scale));

        using var scaled = new Bitmap(width, scaledHeight, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.Clear(Color.White);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, width, scaledHeight);
        }

        var safeSegment = Math.Clamp(segmentHeight, 420, 2200);
        for (var y = 0; y < scaledHeight; y += safeSegment)
        {
            var h = Math.Min(safeSegment, scaledHeight - y);
            using var chunk = new Bitmap(width, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(chunk))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(scaled, new Rectangle(0, 0, width, h), new Rectangle(0, y, width, h), GraphicsUnit.Pixel);
            }
            var raster = BuildRaster(chunk);
            if (raster.Length > 0) list.Add(raster);
        }

        return list;
    }

    private static byte[] BuildRaster(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        if (width <= 0 || height <= 0) return Array.Empty<byte>();

        var widthBytes = (width + 7) / 8;
        var output = new List<byte>(8 + (widthBytes * height))
        {
            0x1D, 0x76, 0x30, 0x00,
            (byte)(widthBytes & 0xFF), (byte)((widthBytes >> 8) & 0xFF),
            (byte)(height & 0xFF), (byte)((height >> 8) & 0xFF),
        };

        var luma = new double[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                luma[(y * width) + x] = ((pixel.R * 299) + (pixel.G * 587) + (pixel.B * 114)) / 1000.0;
            }
        }

        const double threshold = 160.0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width) + x;
                var oldPixel = Math.Clamp(luma[idx], 0.0, 255.0);
                var newPixel = oldPixel < threshold ? 0.0 : 255.0;
                luma[idx] = newPixel;
                var error = oldPixel - newPixel;
                if (x + 1 < width) luma[idx + 1] += error * (7.0 / 16.0);
                if (y + 1 < height)
                {
                    if (x > 0) luma[idx + width - 1] += error * (3.0 / 16.0);
                    luma[idx + width] += error * (5.0 / 16.0);
                    if (x + 1 < width) luma[idx + width + 1] += error * (1.0 / 16.0);
                }
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var xb = 0; xb < widthBytes; xb++)
            {
                byte slice = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    var x = xb * 8 + bit;
                    if (x >= width) continue;
                    if (luma[(y * width) + x] < 128.0)
                    {
                        slice |= (byte)(0x80 >> bit);
                    }
                }
                output.Add(slice);
            }
        }

        return output.ToArray();
    }

    private static IEnumerable<byte> CmdInit() => new byte[] { 0x1B, 0x40 };
    private static IEnumerable<byte> CmdAlign(int mode) => new byte[] { 0x1B, 0x61, (byte)Math.Clamp(mode, 0, 2) };
    private static IEnumerable<byte> CmdBold(bool on) => new byte[] { 0x1B, 0x45, (byte)(on ? 1 : 0) };
    private static IEnumerable<byte> CmdFeed(int lines) => new byte[] { 0x1B, 0x64, (byte)Math.Clamp(lines, 0, 10) };

    private static IEnumerable<byte> OpenDrawer()
    {
        return new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
    }

    private static IEnumerable<byte> Cut(string? mode)
    {
        if (string.Equals(mode, "full", StringComparison.OrdinalIgnoreCase))
        {
            return new byte[] { 0x1D, 0x56, 0x00 };
        }
        return new byte[] { 0x1D, 0x56, 0x01 };
    }

    private static void AppendLine(List<byte> target, string line)
    {
        var value = (line ?? string.Empty).TrimEnd();
        target.AddRange(Enc.GetBytes(value));
        target.Add(0x0A);
    }
}
