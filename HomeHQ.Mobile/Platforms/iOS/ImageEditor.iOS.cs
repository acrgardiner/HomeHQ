using CoreGraphics;
using Foundation;
using UIKit;

namespace HomeHQ.Mobile.Services;

public static partial class ImageEditor
{
    private static partial (int Width, int Height) PlatformGetDimensions(byte[] bytes)
    {
        using var image = UIImage.LoadFromData(NSData.FromArray(bytes));
        var cg = image?.CgImage ?? throw new InvalidOperationException("Could not decode image.");
        return (cg.Width, cg.Height);
    }

    private static partial byte[] PlatformRotate(byte[] bytes, int degrees, string? contentType)
    {
        using var image = UIImage.LoadFromData(NSData.FromArray(bytes));
        if (image == null)
        {
            throw new InvalidOperationException("Could not decode image.");
        }

        using var rotated = Rotate(image, degrees);
        return EncodeUIImage(rotated, contentType);
    }

    private static partial byte[] PlatformCrop(byte[] bytes, ImageCropRectangle crop, string? contentType)
    {
        using var image = UIImage.LoadFromData(NSData.FromArray(bytes));
        if (image == null)
        {
            throw new InvalidOperationException("Could not decode image.");
        }

        using var cgIn = image.CgImage ?? throw new InvalidOperationException("Could not decode image.");
        var rect = new CGRect(crop.X, crop.Y, crop.Width, crop.Height);
        using var cgOut = cgIn.WithImageInRect(rect);
        using var cropped = UIImage.FromImage(cgOut, image.CurrentScale, image.Orientation);
        return EncodeUIImage(cropped, contentType);
    }

    private static UIImage Rotate(UIImage image, int degrees)
    {
        //TODO: Fix
        var size = image.Size;
        UIGraphics.BeginImageContextWithOptions(new CGSize(size.Height, size.Width), false, image.CurrentScale);
        var ctx = UIGraphics.GetCurrentContext();
        if (ctx == null)
        {
            UIGraphics.EndImageContext();
            return image;
        }

        ctx.TranslateCTM(size.Height, 0);
        ctx.RotateCTM((nfloat)(degrees * Math.PI / 180));
        image.Draw(new CGRect(0, 0, size.Width, size.Height));
        var result = UIGraphics.GetImageFromCurrentImageContext();
        UIGraphics.EndImageContext();
        return result ?? image;
    }

    private static byte[] EncodeUIImage(UIImage image, string? contentType)
    {
        var png = contentType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true;
        using var data = png ? image.AsPNG() : image.AsJPEG(0.92f);
        if (data == null)
        {
            throw new InvalidOperationException("Could not encode image.");
        }

        var len = (int)data.Length;
        var bytes = new byte[len];
        System.Runtime.InteropServices.Marshal.Copy(data.Bytes, bytes, 0, len);
        return bytes;
    }
}
