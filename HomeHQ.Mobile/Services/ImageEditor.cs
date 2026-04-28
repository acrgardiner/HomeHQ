using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// In-memory rotate/crop for attachment preview editing.
/// </summary>
public static class ImageEditor
{
    public static (int Width, int Height) GetDimensions(byte[] bytes)
    {
        using var image = Image.Load<Rgba32>(bytes);
        return (image.Width, image.Height);
    }

    public static byte[] RotateClockwise90(byte[] bytes, string? contentType)
    {
        using var image = Image.Load<Rgba32>(bytes);
        image.Mutate(ctx => ctx.Rotate(RotateMode.Rotate90));
        return Encode(image, contentType);
    }

    public static byte[] Crop(byte[] bytes, Rectangle crop, string? contentType)
    {
        using var image = Image.Load<Rgba32>(bytes);
        var bounds = new Rectangle(0, 0, image.Width, image.Height);
        crop = Rectangle.Intersect(crop, bounds);
        if (crop.Width < 1 || crop.Height < 1)
        {
            throw new ArgumentException("Crop rectangle is empty or outside the image.", nameof(crop));
        }

        image.Mutate(ctx => ctx.Crop(crop));
        return Encode(image, contentType);
    }

    private static byte[] Encode(Image<Rgba32> image, string? contentType)
    {
        using var ms = new MemoryStream();
        var asPng = contentType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true;
        if (asPng)
        {
            image.SaveAsPng(ms);
        }
        else
        {
            image.SaveAsJpeg(ms, new JpegEncoder { Quality = 92 });
        }

        return ms.ToArray();
    }
}
