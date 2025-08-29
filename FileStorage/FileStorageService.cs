using Microsoft.AspNetCore.Components.Forms;
using projectaardvarkx2.Entities;
using projectaardvarkx2.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using Image = SixLabors.ImageSharp.Image;
using projectaardvarkx2.Services;

namespace projectaardvarkx2.FileStorage
{
    public class FileStorageService : IFileStorageService
    {
        private long maxFileSize = 1024 * 1024 * 15;

        private readonly int jpegQuality = 80;
        private readonly int webpQuality = 80;
        private readonly PngCompressionLevel pngCompressionLevel = PngCompressionLevel.BestCompression;

        private readonly ILogger<FileStorageService> _logger;
        private readonly IThumbnailService _thumbnailService;

        public FileStorageService(ILogger<FileStorageService> logger, IThumbnailService thumbnailService)
        {
            _logger = logger;
            _thumbnailService = thumbnailService;
        }

        public async Task<string> UploadAsync<T>(IBrowserFile? file) where T : class
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file), "File cannot be null.");
            try
            {
                var uniqueFileName = Guid.NewGuid().ToString();
                var attachmentDir = Path.Combine("appdata", "attachments", typeof(T).Name);

                var fileExtension = Path.GetExtension(file.Name);
                var attachmentFileName = uniqueFileName + fileExtension;

                // Save file to disk or database as needed
                var fullFilePath = Path.Combine(attachmentDir, attachmentFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath)!);

                using (var stream = file.OpenReadStream(maxFileSize))
                {
                    using (var fileStream = new FileStream(fullFilePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    // Generate Thumbnail if it's an image file
                    if (IsImageFile(file.ContentType))
                    {
                        await _thumbnailService.GenerateThumbnailAsync<T>(fullFilePath);
                    }
                }

                return attachmentFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading: {FileName}", file?.Name);
                throw new InvalidOperationException("Upload failed.", ex);
            }
        }

        public async Task<(string, float)> UploadAsync<T>(byte[]? fileBytes, string sourceFilename, string contentType) where T : class
        {
            try
            {
                if (fileBytes == null || fileBytes.Length == 0)
                    throw new ArgumentException("File bytes cannot be null or empty.", nameof(fileBytes));

                var uniqueFileName = Guid.NewGuid().ToString();
                var attachmentDir = Path.Combine("appdata", "attachments", typeof(T).Name);

                var fileExtension = Path.GetExtension(sourceFilename);
                var attachmentFileName = uniqueFileName + fileExtension;
                float savedFileSize = fileBytes.Length;

                // Save file to disk or database as needed
                var fullFilePath = Path.Combine(attachmentDir, attachmentFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath)!);

                if (IsImageFile(contentType))
                {
                    // Save image with compression
                    savedFileSize = await SaveCompressedImageAsync(fileBytes, fullFilePath, contentType);

                    // Generate Thumbnail if it's an image file
                    await _thumbnailService.GenerateThumbnailAsync<T>(fullFilePath);
                }
                else
                {
                    File.WriteAllBytes(fullFilePath, fileBytes);
                }

                    return (attachmentFileName, savedFileSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading {0}", sourceFilename);
                throw new InvalidOperationException("Upload failed.", ex);
            }
        }

        public async Task<(string, float)> ReUploadAsync<T>(Attachment attachment, byte[]? fileBytes) where T : class
        {
            try
            {
                if (fileBytes == null || fileBytes.Length == 0)
                    throw new ArgumentException("File bytes cannot be null or empty.", nameof(fileBytes));

                //Use existing filename, but add numbered suffix to avoid collisions
                int i = 0;
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(attachment.LocalFileName);
                string uniqueFileName;
                string attachmentFileName;

                do {
                    uniqueFileName = $"{fileNameWithoutExt}_{++i}";
                    attachmentFileName = uniqueFileName + attachment.Extension;
                } while (File.Exists(Path.Combine("appdata", "attachments", typeof(T).Name, attachmentFileName)));

                var attachmentDir = Path.Combine("appdata", "attachments", typeof(T).Name);

                //var fileExtension = attachment.Extension;
                float savedFileSize = fileBytes.Length;

                // Save file to disk or database as needed
                var fullFilePath = Path.Combine(attachmentDir, attachmentFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath)!);

                if (IsImageFile(attachment.ContentType))
                {
                    // Save image with compression
                    savedFileSize = await SaveCompressedImageAsync(fileBytes, fullFilePath, attachment.ContentType);

                    // Generate Thumbnail if it's an image file
                    await _thumbnailService.GenerateThumbnailAsync<T>(fullFilePath);
                }
                else
                {
                    File.WriteAllBytes(fullFilePath, fileBytes);
                }

                return (attachmentFileName, savedFileSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-uploading {0}", attachment.Id);
                throw new InvalidOperationException("Upload failed.", ex);
            }
        }

        public async Task<string> UploadAsync<T>(string localFile, string contentType) where T : class
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
                if (IsImageFile(contentType))
                {
                    await _thumbnailService.GenerateThumbnailAsync<T>(fullFilePath);
                }

                return attachmentFileName;
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
                return false;

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
                        await image.SaveAsJpegAsync(filePath, new JpegEncoder { Quality = jpegQuality });
                        savedSize = new FileInfo(filePath).Length;
                        break;
                    case ".png":
                        await image.SaveAsPngAsync(filePath, new PngEncoder { CompressionLevel = pngCompressionLevel });
                        savedSize = new FileInfo(filePath).Length;
                        break;
                    case ".webp":
                        await image.SaveAsWebpAsync(filePath, new WebpEncoder { Quality = webpQuality });
                        savedSize = new FileInfo(filePath).Length;
                        break;
                    default:
                        // Fallback to JPEG for unknown image formats
                        var jpegPath = Path.ChangeExtension(filePath, ".jpg");
                        await image.SaveAsJpegAsync(jpegPath, new JpegEncoder { Quality = jpegQuality });
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
}
