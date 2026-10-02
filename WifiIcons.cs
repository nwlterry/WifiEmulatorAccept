using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WifiEmulatorAccept;

public static class WifiIcons
{
    static readonly Color ConnectedTile = Color.FromArgb(15, 108, 189);
    static readonly Color DisconnectedTile = Color.FromArgb(96, 104, 112);
    static readonly Color Glyph = Color.White;

    public static Icon Tile(bool connected, int size) => FromBitmap(DrawTile(size, connected));

    public static void SaveAppIcon(string path)
    {
        var frames = new[] { 16, 24, 32, 48, 64, 256 }.Select(size => DrawTile(size, connected: true)).ToList();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = File.Create(path);
            WriteIco(stream, frames);
        }
        finally
        {
            foreach (var frame in frames)
                frame.Dispose();
        }
    }

    public static void SavePreview(string path, bool connected, int size)
    {
        using var bitmap = DrawTile(size, connected);
        bitmap.Save(path, ImageFormat.Png);
    }

    static Bitmap DrawTile(int size, bool connected)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        var tile = RectangleF.Inflate(new RectangleF(0, 0, size, size), -0.5f, -0.5f);
        using (var background = new SolidBrush(connected ? ConnectedTile : DisconnectedTile))
        using (var shape = RoundRect(tile, size * 0.22f))
            graphics.FillPath(background, shape);

        DrawGlyph(graphics, size, Glyph);
        if (!connected)
            DrawOffSlash(graphics, size);
        return bitmap;
    }

    static void DrawGlyph(Graphics graphics, int size, Color color)
    {
        var penWidth = Math.Max(1.4f, size * 0.07f);
        using var pen = new Pen(color, penWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        using var dot = new SolidBrush(color);
        var cx = size * 0.50f;
        var cy = size * 0.70f;
        var dotRadius = Math.Max(1.2f, size * 0.055f);
        graphics.FillEllipse(dot, cx - dotRadius, cy - dotRadius, dotRadius * 2, dotRadius * 2);

        float[] radii = size < 24
            ? [size * 0.20f, size * 0.36f]
            : [size * 0.16f, size * 0.30f, size * 0.44f];
        foreach (var radius in radii)
            graphics.DrawArc(pen, cx - radius, cy - radius, radius * 2, radius * 2, 205, 130);
    }

    static void DrawOffSlash(Graphics graphics, int size)
    {
        var penWidth = Math.Max(1.8f, size * 0.075f);
        using var outline = new Pen(DisconnectedTile, penWidth + Math.Max(1.5f, size * 0.045f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        using var slash = new Pen(Color.White, penWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        var x1 = size * 0.30f;
        var y1 = size * 0.70f;
        var x2 = size * 0.72f;
        var y2 = size * 0.28f;
        graphics.DrawLine(outline, x1, y1, x2, y2);
        graphics.DrawLine(slash, x1, y1, x2, y2);
    }

    static GraphicsPath RoundRect(RectangleF bounds, float radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    static Icon FromBitmap(Bitmap bitmap)
    {
        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    static void WriteIco(Stream stream, IReadOnlyList<Bitmap> frames)
    {
        var pngs = new List<byte[]>(frames.Count);
        foreach (var frame in frames)
        {
            using var png = new MemoryStream();
            frame.Save(png, ImageFormat.Png);
            pngs.Add(png.ToArray());
        }

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)frames.Count);
        var offset = 6 + (16 * frames.Count);
        for (var i = 0; i < frames.Count; i++)
        {
            var width = frames[i].Width >= 256 ? 0 : frames[i].Width;
            var height = frames[i].Height >= 256 ? 0 : frames[i].Height;
            writer.Write((byte)width);
            writer.Write((byte)height);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(pngs[i].Length);
            writer.Write(offset);
            offset += pngs[i].Length;
        }

        foreach (var png in pngs)
            writer.Write(png);
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyIcon(IntPtr handle);
}
