using HomeHQ.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using Image = SixLabors.ImageSharp.Image;
using HomeHQ.Services;

namespace HomeHQ.FileStorage;

public class FileStorageService : IFileStorageService
{

    private readonly int _jpegQuality = 80;
    private readonly int _webpQuality = 80;
    private readonly PngCompressionLevel _pngCompressionLevel = PngCompressionLevel.BestCompression;

    private readonly ILogger<FileStorageService> _logger;
    private readonly IThumbnailService _thumbnailService;

    public FileStorageService(ILogger<FileStorageService> logger, IThumbnailService thumbnailService)
    {
        _logger = logger;
        _thumbnailService = thumbnailService;
    }



    public async Task<bool> UploadAsync<T>(byte[]? fileBytes, Attachment attachment) where T : class
    {
        try
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new ArgumentException("File bytes cannot be null or empty.", nameof(fileBytes));
            }

            var uniqueFileName = Guid.NewGuid().ToString();
            var attachmentDir = Path.Combine("appdata", "attachments", typeof(T).Name);

            var fileExtension = Path.GetExtension(attachment.OriginFileName);
            var attachmentFileName = uniqueFileName + fileExtension;
            float savedFileSize = fileBytes.Length;

            // Save file to disk or database as needed
            var fullFilePath = Path.Combine(attachmentDir, attachmentFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath)!);

            if (IsImageFile(attachment.ContentType))
            {
                // Save image with compression
                savedFileSize = await SaveCompressedImageAsync(fileBytes, fullFilePath, attachment.ContentType);

                // Generate Thumbnail if it's an image file
                var thumbMetadata = await _thumbnailService.GenerateThumbnailAsync<T>(fullFilePath);
                if (thumbMetadata != null)
                {
                    attachment.Thumb_LocalFileName = thumbMetadata.LocalFileName;
                    attachment.Thumb_ContentType = thumbMetadata.ContentType;
                    attachment.Thumb_Extension = thumbMetadata.Extension;
                    attachment.Thumb_FileSize = thumbMetadata.FileSize;
                }
            }
            else
            {
                File.WriteAllBytes(fullFilePath, fileBytes);
            }

            attachment.LocalFileName = attachmentFileName;
            attachment.FileSize = savedFileSize;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading {0}", attachment.OriginFileName);
            throw new InvalidOperationException("Upload failed.", ex);
        }
    }

    public async Task<bool> UploadAsync<T>(string localFile, Attachment attachment) where T : class
    {
        try
        {
            var uniqueFileName = Guid.NewGuid().ToString();
            var attachmentDir = Path.Combine("appdata", "attachments", typeof(T).Name);

            var fileExtension = Path.GetExtension(localFile);
            var attachmentFileName = uniqueFileName + fileExtension;

            // Save file to disk or database as needed
            var fullFilePath = Path.Combine(attachmentDir, attachmentFileName);

            Directory.CreateDirectory(attachmentDir);

            File.Move(localFile, fullFilePath);

            // Generate Thumbnail if it's an image file
            if (IsImageFile(attachment.ContentType))
            {
                var thumbMetadata = await _thumbnailService.GenerateThumbnailAsync<T>(fullFilePath);
                if (thumbMetadata != null)
                {
                    attachment.Thumb_LocalFileName = thumbMetadata.LocalFileName;
                    attachment.Thumb_ContentType = thumbMetadata.ContentType;
                    attachment.Thumb_Extension = thumbMetadata.Extension;
                    attachment.Thumb_FileSize = thumbMetadata.FileSize;
                }
            }

            attachment.LocalFileName = attachmentFileName;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading: {FileName}", localFile);
            throw new InvalidOperationException("Upload failed.", ex);
        }
    }

    private bool IsImageFile(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        var imageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
        return imageTypes.Contains(contentType.ToLower());
    }

    private async Task<float> SaveCompressedImageAsync(byte[] imageBytes, string filePath, string contentType)
    {
        try
        {
            using var image = Image.Load(imageBytes);
            var extension = Path.GetExtension(filePath).ToLower();
            float savedSize = 0;
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    await image.SaveAsJpegAsync(filePath, new JpegEncoder { Quality = _jpegQuality });
                    savedSize = new FileInfo(filePath).Length;
                    break;
                case ".png":
                    await image.SaveAsPngAsync(filePath, new PngEncoder { CompressionLevel = _pngCompressionLevel });
                    savedSize = new FileInfo(filePath).Length;
                    break;
                case ".webp":
                    await image.SaveAsWebpAsync(filePath, new WebpEncoder { Quality = _webpQuality });
                    savedSize = new FileInfo(filePath).Length;
                    break;
                default:
                    // Fallback to JPEG for unknown image formats
                    var jpegPath = Path.ChangeExtension(filePath, ".jpg");
                    await image.SaveAsJpegAsync(jpegPath, new JpegEncoder { Quality = _jpegQuality });
                    savedSize = new FileInfo(jpegPath).Length;
                    break;
            }

            _logger.LogInformation("Compressed image saved: {FilePath}", filePath);
            return savedSize;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save compressed image, falling back to raw bytes: {FilePath}", filePath);
            // Fallback to saving raw bytes if image processing fails
            File.WriteAllBytes(filePath, imageBytes);
        }
        return 0;
    }
}
