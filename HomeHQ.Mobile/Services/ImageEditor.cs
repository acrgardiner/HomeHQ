namespace HomeHQ.Mobile.Services;

/// <summary>
/// Pixel rectangle in bitmap coordinates (used by crop).
/// </summary>
public readonly record struct ImageCropRectangle(int X, int Y, int Width, int Height);

/// <summary>
/// In-memory rotate, crop, and color adjustments using each platform's bitmap APIs — no extra NuGet packages.
/// </summary>
public static partial class ImageEditor
{
    public static (int Width, int Height) GetDimensions(byte[] bytes) =>
        PlatformGetDimensions(bytes);

    public static byte[] Rotate(byte[] bytes, int degrees, string? contentType) =>
        PlatformRotate(bytes, degrees, contentType);

    public static byte[] Crop(byte[] bytes, ImageCropRectangle crop, string? contentType) =>
        PlatformCrop(bytes, crop, contentType);

    public static byte[] Grayscale(byte[] bytes, string? contentType)
    {
        var pixels = PlatformGetArgbPixels(bytes, out var width, out var height);
        ImagePixels.ApplyGrayscale(pixels);
        return PlatformEncodeArgbPixels(pixels, width, height, contentType);
    }

    /// <param name="amount">1 = unchanged; values above 1 increase contrast.</param>
    public static byte[] Contrast(byte[] bytes, float amount, string? contentType)
    {
        var pixels = PlatformGetArgbPixels(bytes, out var width, out var height);
        ImagePixels.ApplyContrast(pixels, amount);
        return PlatformEncodeArgbPixels(pixels, width, height, contentType);
    }

    /// <param name="amount">0 = unchanged; 1 is a typical sharpen pass.</param>
    public static byte[] Sharpen(byte[] bytes, float amount, string? contentType)
    {
        var pixels = PlatformGetArgbPixels(bytes, out var width, out var height);
        ImagePixels.ApplySharpen(pixels, width, height, amount);
        return PlatformEncodeArgbPixels(pixels, width, height, contentType);
    }

    private static partial (int Width, int Height) PlatformGetDimensions(byte[] bytes);

    private static partial byte[] PlatformRotate(byte[] bytes, int degrees, string? contentType);

    private static partial byte[] PlatformCrop(byte[] bytes, ImageCropRectangle crop, string? contentType);

    private static partial int[] PlatformGetArgbPixels(byte[] bytes, out int width, out int height);

    private static partial byte[] PlatformEncodeArgbPixels(int[] pixels, int width, int height, string? contentType);
}
