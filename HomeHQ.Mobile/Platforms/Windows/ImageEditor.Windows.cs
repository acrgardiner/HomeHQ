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
