namespace HomeHQ.Mobile.Services;

/// <summary>
/// Packed ARGB pixel ops shared by all <see cref="ImageEditor"/> platforms.
/// Each int is <c>(A &lt;&lt; 24) | (R &lt;&lt; 16) | (G &lt;&lt; 8) | B</c>.
/// </summary>
internal static class ImagePixels
{
    public static void ApplyGrayscale(int[] pixels)
    {
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var a = (p >> 24) & 0xFF;
            var r = (p >> 16) & 0xFF;
            var g = (p >> 8) & 0xFF;
            var b = p & 0xFF;
            var y = (r * 77 + g * 150 + b * 29) >> 8;
            pixels[i] = (a << 24) | (y << 16) | (y << 8) | y;
        }
    }

    /// <param name="amount">1 = unchanged; &lt;1 reduces contrast; &gt;1 increases it.</param>
    public static void ApplyContrast(int[] pixels, float amount)
    {
        if (Math.Abs(amount - 1f) < 0.001f)
        {
            return;
        }

        amount = Math.Clamp(amount, 0.1f, 3f);
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var a = (p >> 24) & 0xFF;
            var r = ContrastChannel((p >> 16) & 0xFF, amount);
            var g = ContrastChannel((p >> 8) & 0xFF, amount);
            var b = ContrastChannel(p & 0xFF, amount);
            pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }
    }

    /// <param name="amount">0 = unchanged; 1 ≈ a standard unsharp pass.</param>
    public static void ApplySharpen(int[] pixels, int width, int height, float amount)
    {
        if (amount <= 0.001f || width < 3 || height < 3)
        {
            return;
        }

        amount = Math.Clamp(amount, 0f, 3f);
        var src = (int[])pixels.Clone();
        var centerWeight = 1f + (4f * amount);

        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            for (var x = 0; x < width; x++)
            {
                var i = row + x;
                var center = src[i];
                var a = (center >> 24) & 0xFF;

                Sample(src, width, height, x, y, out var cr, out var cg, out var cb);
                Sample(src, width, height, x, y - 1, out var nr, out var ng, out var nb);
                Sample(src, width, height, x, y + 1, out var sr, out var sg, out var sb);
                Sample(src, width, height, x - 1, y, out var wr, out var wg, out var wb);
                Sample(src, width, height, x + 1, y, out var er, out var eg, out var eb);

                var r = ClampByte((centerWeight * cr) - (amount * (nr + sr + wr + er)));
                var g = ClampByte((centerWeight * cg) - (amount * (ng + sg + wg + eg)));
                var b = ClampByte((centerWeight * cb) - (amount * (nb + sb + wb + eb)));
                pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }
    }

    private static int ContrastChannel(int value, float amount) =>
        ClampByte(((value - 128) * amount) + 128.5f);

    private static void Sample(int[] src, int width, int height, int x, int y, out int r, out int g, out int b)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        var p = src[(y * width) + x];
        r = (p >> 16) & 0xFF;
        g = (p >> 8) & 0xFF;
        b = p & 0xFF;
    }

    private static int ClampByte(float value) =>
        (int)Math.Clamp(value, 0f, 255f);
}
