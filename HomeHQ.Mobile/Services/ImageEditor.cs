namespace HomeHQ.Mobile.Services;

/// <summary>
/// Pixel rectangle in bitmap coordinates (used by crop).
/// </summary>
public readonly record struct ImageCropRectangle(int X, int Y, int Width, int Height);

/// <summary>
/// In-memory rotate/crop using each platform's bitmap APIs — no extra NuGet packages.
/// </summary>
public static partial class ImageEditor
{
    public static (int Width, int Height) GetDimensions(byte[] bytes) =>
        PlatformGetDimensions(bytes);

    public static byte[] Rotate(byte[] bytes, int degrees, string? contentType) =>
        PlatformRotate(bytes, degrees, contentType);

    public static byte[] Crop(byte[] bytes, ImageCropRectangle crop, string? contentType) =>
        PlatformCrop(bytes, crop, contentType);

    private static partial (int Width, int Height) PlatformGetDimensions(byte[] bytes);

    private static partial byte[] PlatformRotate(byte[] bytes, int degrees, string? contentType);

    private static partial byte[] PlatformCrop(byte[] bytes, ImageCropRectangle crop, string? contentType);
}
