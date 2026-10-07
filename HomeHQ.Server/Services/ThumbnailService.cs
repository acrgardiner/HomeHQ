
using HomeHQ.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using PDFtoImage;
using Image = SixLabors.ImageSharp.Image;

namespace HomeHQ.Services;

public interface IThumbnailService
{
    Task<int> RebuildAll();
    Task<ThumbnailMetadata?> GenerateThumbnailAsync<T>(string sourceFile, string contentType);
}

public class ThumbnailMetadata
{
    public string LocalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public string Extension { get; set; } = ".jpg";
    public float FileSize { get; set; }
}

public class ThumbnailService : IThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;

    private readonly IEntityService<Asset> _assetService;
    private readonly IEntityService<Attachment> _attachmentService;

    private readonly int[] _thumbnailDimentions = [1200, 1200]; //[width, height]
    private readonly int _thumbnailJpegQuality = 80;

    public ThumbnailService(
        ILogger<ThumbnailService> logger,
        IEntityService<Asset> assetService,
        IEntityService<Attachment> attachmentService
    )
    {
        _logger = logger;
        _assetService = assetService;
        _attachmentService = attachmentService;
    }

    public async Task<int> RebuildAll()
    {
        var allAssets = await _assetService.GetAllAsync();
        var assetAttachmentDir = Path.Combine("appdata", "attachments", typeof(Asset).Name);
        var assetThumbsDir = Path.Combine("appdata", "thumbs", typeof(Asset).Name);

        ClearDirectory(assetThumbsDir);

        int processedCount = 0;
        foreach (var asset in allAssets)
        {
            var attachments = _attachmentService.GetAsync(a => a.ParentId == asset.Id).Result;
            foreach (var attachment in attachments)
            {
                if (attachment is null)
                {
                    continue;
                }

                if (IsImageFile(attachment.ContentType) || IsPDFFile(attachment.ContentType))
                {
                    string attachmentFile = Path.Combine(assetAttachmentDir, attachment.LocalFileName);
                    var thumbMetadata = await GenerateThumbnailAsync<Asset>(attachmentFile, attachment.ContentType);
                    if (thumbMetadata != null)
                    {
                        attachment.Thumb_LocalFileName = thumbMetadata.LocalFileName;
                        attachment.Thumb_ContentType = thumbMetadata.ContentType;
                        attachment.Thumb_Extension = thumbMetadata.Extension;
                        attachment.Thumb_FileSize = thumbMetadata.FileSize;
                    }

                    processedCount++;
                }
            }

            await _attachmentService.UpdateAsync(attachments);
        }

        return processedCount;
    }

    public async Task<ThumbnailMetadata?> GenerateThumbnailAsync<T>(string sourceFile, string contentType)
    {
        try
        {
            var thumbsDir = Path.Combine("appdata", "thumbs", typeof(T).Name);
            var thumbnailFilename = Path.GetFileNameWithoutExtension(sourceFile) + "_thumb.jpg";

            Directory.CreateDirectory(thumbsDir);

            var thumbnailFile = Path.Combine(thumbsDir, thumbnailFilename);

            //If Is Image
            if (IsImageFile(contentType.ToString()))
            {
                using var image = await Image.LoadAsync(sourceFile);
                // Calculate thumbnail dimensions while maintaining aspect ratio
                var (thumbWidth, thumbHeight) = CalculateThumbnailDimensions(image.Width, image.Height, _thumbnailDimentions[0], _thumbnailDimentions[1]);

                // Create thumbnail
                image.Mutate(x => x.Resize(thumbWidth, thumbHeight));

                await image.SaveAsJpegAsync(thumbnailFile, new JpegEncoder { Quality = _thumbnailJpegQuality });
            }
            else if (IsPDFFile(contentType.ToString()))
            {
                // Generate thumbnail from first page of PDF
                using var pdf = File.OpenRead(sourceFile);
                //using var pdfPage = pdfDocument.Render(0, 300, 300, true);

                //using var image = Image.Load(pdfPage);
                PDFtoImage.Conversion.SaveJpeg(
                    imageFilename: thumbnailFile,
                    pdfStream: pdf,
                    page: 0,
                    options: new RenderOptions
                    {
                        Dpi = 96,
                        Width = _thumbnailDimentions[0],
                        Height = null,
                        WithAspectRatio = true,
                    });
            }

            var fileInfo = new FileInfo(thumbnailFile);

            var metadata = new ThumbnailMetadata
            {
                LocalFileName = thumbnailFilename,
                ContentType = "image/jpeg",
                Extension = ".jpg",
                FileSize = fileInfo.Length
            };

            _logger.LogInformation("Thumbnail generated: {ThumbnailPath}", thumbnailFile);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate thumbnail for: {0}", sourceFile);
            return null;
        }
    }

    private static (int width, int height) CalculateThumbnailDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var ratioX = (double)maxWidth / originalWidth;
        var ratioY = (double)maxHeight / originalHeight;
        var ratio = Math.Min(ratioX, ratioY);

        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }

    private static bool IsImageFile(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        var imageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" }; // TODO: Move to static
        return imageTypes.Contains(contentType.ToLower());
    }

    private static bool IsPDFFile(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    private void ClearDirectory(string directory)
    {
        // Check if the directory exists
        if (!Directory.Exists(directory))
        {
            _logger.LogDebug($"Directory '{directory}' does not exist.");
            return;
        }

        try
        {
            // Get all files in the specified directory
            string[] files = Directory.GetFiles(directory);

            // Iterate through each file and delete it
            foreach (string file in files)
            {
                File.Delete(file);
                _logger.LogDebug("Deleted file: {0}", file);
            }

            _logger.LogDebug("All files in '{0}' have been deleted.", directory);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "An error occurred: {0}", ex.Message);
        }
    }
}
