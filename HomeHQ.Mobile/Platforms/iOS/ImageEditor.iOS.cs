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

    private static partial int[] PlatformGetArgbPixels(byte[] bytes, out int width, out int height)
    {
        using var image = UIImage.LoadFromData(NSData.FromArray(bytes));
        using var cg = image?.CgImage ?? throw new InvalidOperationException("Could not decode image.");
        width = (int)cg.Width;
        height = (int)cg.Height;
        var rgba = new byte[width * height * 4];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(rgba, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            using var colorSpace = CGColorSpace.CreateDeviceRGB();
            using var ctx = new CGBitmapContext(
                handle.AddrOfPinnedObject(),
                width,
                height,
                8,
                width * 4,
                colorSpace,
                CGImageAlphaInfo.PremultipliedLast);
            if (ctx == null)
            {
                throw new InvalidOperationException("Could not decode image.");
            }

            ctx.DrawImage(new CGRect(0, 0, width, height), cg);
        }
        finally
        {
            handle.Free();
        }

        var pixels = new int[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            var r = rgba[o];
            var g = rgba[o + 1];
            var b = rgba[o + 2];
            var a = rgba[o + 3];
            pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }

        return pixels;
    }

    private static partial byte[] PlatformEncodeArgbPixels(int[] pixels, int width, int height, string? contentType)
    {
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var o = i * 4;
            rgba[o] = (byte)((p >> 16) & 0xFF);
            rgba[o + 1] = (byte)((p >> 8) & 0xFF);
            rgba[o + 2] = (byte)(p & 0xFF);
            rgba[o + 3] = (byte)((p >> 24) & 0xFF);
        }

        var handle = System.Runtime.InteropServices.GCHandle.Alloc(rgba, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            using var colorSpace = CGColorSpace.CreateDeviceRGB();
            using var ctx = new CGBitmapContext(
                handle.AddrOfPinnedObject(),
                width,
                height,
                8,
                width * 4,
                colorSpace,
                CGImageAlphaInfo.PremultipliedLast);
            using var cg = ctx?.ToImage() ?? throw new InvalidOperationException("Could not encode image.");
            using var image = UIImage.FromImage(cg);
            return EncodeUIImage(image, contentType);
        }
        finally
        {
            handle.Free();
        }
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
