using Android.Graphics;

namespace HomeHQ.Mobile.Services;

public static partial class ImageEditor
{
    private static partial (int Width, int Height) PlatformGetDimensions(byte[] bytes)
    {
        var opts = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, opts);
        return (opts.OutWidth, opts.OutHeight);
    }

    private static partial byte[] PlatformRotate(byte[] bytes, int degrees, string? contentType)
    {
        using var src = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length)
            ?? throw new InvalidOperationException("Could not decode image.");
        using var matrix = new Matrix();
        matrix.PostRotate(degrees);
        using var rotated = Bitmap.CreateBitmap(src, 0, 0, src.Width, src.Height, matrix, true)
            ?? throw new InvalidOperationException("Rotate failed.");
        return EncodeBitmap(rotated, contentType);
    }

    private static partial byte[] PlatformCrop(byte[] bytes, ImageCropRectangle crop, string? contentType)
    {
        using var src = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length)
            ?? throw new InvalidOperationException("Could not decode image.");

        var x = Math.Clamp(crop.X, 0, Math.Max(0, src.Width - 1));
        var y = Math.Clamp(crop.Y, 0, Math.Max(0, src.Height - 1));
        var w = Math.Clamp(crop.Width, 1, src.Width - x);
        var h = Math.Clamp(crop.Height, 1, src.Height - y);

        using var cropped = Bitmap.CreateBitmap(src, x, y, w, h)
            ?? throw new InvalidOperationException("Crop failed.");
        return EncodeBitmap(cropped, contentType);
    }

    private static byte[] EncodeBitmap(Bitmap bmp, string? contentType)
    {
        using var ms = new MemoryStream();
        var png = contentType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true;
        var format = png ? Bitmap.CompressFormat.Png : Bitmap.CompressFormat.Jpeg;
        var quality = png ? 100 : 92;
        if (!bmp.Compress(format, quality, ms))
        {
            throw new InvalidOperationException("Could not encode image.");
        }

        return ms.ToArray();
    }
}
