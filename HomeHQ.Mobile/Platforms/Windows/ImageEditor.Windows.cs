using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HomeHQ.Mobile.Services;

public static partial class ImageEditor
{
    private static partial (int Width, int Height) PlatformGetDimensions(byte[] bytes)
    {
        using var stream = new InMemoryRandomAccessStream();
        stream.WriteAsync(bytes.AsBuffer()).AsTask().GetAwaiter().GetResult();
        stream.Seek(0);
        var decoder = BitmapDecoder.CreateAsync(stream).AsTask().GetAwaiter().GetResult();
        return ((int)decoder.PixelWidth, (int)decoder.PixelHeight);
    }

    private static partial byte[] PlatformRotate(byte[] bytes, int degrees, string? contentType)
    {
        //TODO: Fix
        using var input = new InMemoryRandomAccessStream();
        input.WriteAsync(bytes.AsBuffer()).AsTask().GetAwaiter().GetResult();
        input.Seek(0);
        var decoder = BitmapDecoder.CreateAsync(input).AsTask().GetAwaiter().GetResult();
        var transform = new BitmapTransform { Rotation = degrees switch
        {
            90 => BitmapRotation.Clockwise90Degrees,
            180 => BitmapRotation.Clockwise180Degrees,
            270 => BitmapRotation.Clockwise270Degrees,
            _ => BitmapRotation.None
        }};
        return EncodeWithTransform(decoder, transform, contentType);
    }

    private static partial byte[] PlatformCrop(byte[] bytes, ImageCropRectangle crop, string? contentType)
    {
        using var input = new InMemoryRandomAccessStream();
        input.WriteAsync(bytes.AsBuffer()).AsTask().GetAwaiter().GetResult();
        input.Seek(0);
        var decoder = BitmapDecoder.CreateAsync(input).AsTask().GetAwaiter().GetResult();
        var transform = new BitmapTransform
        {
            Bounds = new BitmapBounds
            {
                X = (uint)Math.Max(0, crop.X),
                Y = (uint)Math.Max(0, crop.Y),
                Width = (uint)Math.Max(1, crop.Width),
                Height = (uint)Math.Max(1, crop.Height)
            }
        };

        return EncodeWithTransform(decoder, transform, contentType);
    }

    private static partial int[] PlatformGetArgbPixels(byte[] bytes, out int width, out int height)
    {
        using var input = new InMemoryRandomAccessStream();
        input.WriteAsync(bytes.AsBuffer()).AsTask().GetAwaiter().GetResult();
        input.Seek(0);
        var decoder = BitmapDecoder.CreateAsync(input).AsTask().GetAwaiter().GetResult();
        var pixelData = decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                new BitmapTransform(),
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        width = (int)decoder.OrientedPixelWidth;
        height = (int)decoder.OrientedPixelHeight;
        var bgra = pixelData.DetachPixelData();
        var pixels = new int[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            var b = bgra[o];
            var g = bgra[o + 1];
            var r = bgra[o + 2];
            var a = bgra[o + 3];
            pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }

        return pixels;
    }

    private static partial byte[] PlatformEncodeArgbPixels(int[] pixels, int width, int height, string? contentType)
    {
        var bgra = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var o = i * 4;
            bgra[o] = (byte)(p & 0xFF);
            bgra[o + 1] = (byte)((p >> 8) & 0xFF);
            bgra[o + 2] = (byte)((p >> 16) & 0xFF);
            bgra[o + 3] = (byte)((p >> 24) & 0xFF);
        }

        using var output = new InMemoryRandomAccessStream();
        var png = contentType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true;
        var encoderId = png ? BitmapEncoder.PngEncoderId : BitmapEncoder.JpegEncoderId;
        var encoder = BitmapEncoder.CreateAsync(encoderId, output).AsTask().GetAwaiter().GetResult();
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)width,
            (uint)height,
            96,
            96,
            bgra);
        encoder.FlushAsync().AsTask().GetAwaiter().GetResult();

        output.Seek(0);
        using var reader = new DataReader(output);
        var size = (uint)output.Size;
        reader.LoadAsync(size).AsTask().GetAwaiter().GetResult();
        var result = new byte[size];
        reader.ReadBytes(result);
        return result;
    }

    private static byte[] EncodeWithTransform(BitmapDecoder decoder, BitmapTransform transform, string? contentType)
    {
        throw new NotImplementedException("This method is not implemented yet. It needs to be tested and verified before use.");

        //var pixelProvider = decoder.GetPixelDataAsync(
        //        BitmapPixelFormat.Bgra8,
        //        BitmapAlphaMode.Premultiplied,
        //        transform,
        //        ExifOrientationMode.RespectExifOrientation,
        //        ColorManagementMode.DoNotColorManage)
        //    .AsTask()
        //    .GetAwaiter()
        //    .GetResult();

        //var (pixelWidth, pixelHeight) = GetTransformedPixelDimensions(decoder, transform);

        ////TODO: Fix?
        ////using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
        ////    pixelProvider.DetachPixelData(),
        ////    BitmapPixelFormat.Bgra8,
        ////    pixelWidth,
        ////    pixelHeight,
        ////    BitmapAlphaMode.Premultiplied);

        //using var output = new InMemoryRandomAccessStream();
        //var png = contentType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true;
        //var encoderId = png ? BitmapEncoder.PngEncoderId : BitmapEncoder.JpegEncoderId;
        //var encoder = BitmapEncoder.CreateAsync(encoderId, output).AsTask().GetAwaiter().GetResult();
        //encoder.SetSoftwareBitmap(bitmap);
        //encoder.FlushAsync().AsTask().GetAwaiter().GetResult();

        //output.Seek(0);
        //using var reader = new DataReader(output);
        //var size = (uint)output.Size;
        //reader.LoadAsync(size).AsTask().GetAwaiter().GetResult();
        //var result = new byte[size];
        //reader.ReadBytes(result);
        //return result;
    }

    /// <summary>
    /// PixelDataProvider only exposes the buffer; dimensions come from the decoder + transform.
    /// </summary>
    private static (int PixelWidth, int PixelHeight) GetTransformedPixelDimensions(
        BitmapDecoder decoder,
        BitmapTransform transform)
    {
        var ow = (int)decoder.OrientedPixelWidth;
        var oh = (int)decoder.OrientedPixelHeight;

        var b = transform.Bounds;
        if (b.Width > 0 && b.Height > 0)
        {
            return ((int)b.Width, (int)b.Height);
        }

        return transform.Rotation switch
        {
            BitmapRotation.Clockwise90Degrees or BitmapRotation.Clockwise270Degrees => (oh, ow),
            _ => (ow, oh)
        };
    }
}
