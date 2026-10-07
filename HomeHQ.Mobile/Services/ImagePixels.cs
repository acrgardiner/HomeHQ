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
    public static void ApplyBrightness(int[] pixels, float amount)
    {
        // amount: 0 = unchanged; >0 increases brightness; <0 decreases brightness
        if (Math.Abs(amount) <= 0.001f)
        {
            return;
        }

        // Allow reasonable range but don't blow out values
        amount = Math.Clamp(amount, -3f, 3f);

        // Translate amount to a per-channel offset. A value of 1 results in a modest
        // brightening (~30 units). This keeps changes perceptually reasonable.
        var offset = amount * 30f;

        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var a = (p >> 24) & 0xFF;
            var r = ClampByte(((p >> 16) & 0xFF) + offset);
            var g = ClampByte(((p >> 8) & 0xFF) + offset);
            var b = ClampByte((p & 0xFF) + offset);
            pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
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
