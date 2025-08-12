using Microsoft.AspNetCore.Components.Forms;
using projectaardvarkx2.Entities;
using projectaardvarkx2.Repositories;
using static MudBlazor.CategoryTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Image = SixLabors.ImageSharp.Image;

namespace projectaardvarkx2.FileStorage
{
    public class FileStorageService : IFileStorageService
    {
        private long maxFileSize = 1024 * 1024 * 15;
        private readonly int[] thumbnailDimentions = [600, 600]; //[width, height]

        private readonly IGenericRepository<Attachment> _attachmentRepository;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IGenericRepository<Attachment> attachmentRepository, ILogger<FileStorageService> logger)
        {
            _attachmentRepository = attachmentRepository;
            _logger = logger;
        }

        public async Task<string> UploadAsync<T>(IBrowserFile? file) where T : class
        {
            try
            {
                var uniqueFileName = Guid.NewGuid().ToString();
                var attachmentDir = Path.Combine("appdata", "attachments", typeof(T).Name);
                var thumbsDir = Path.Combine("appdata", "thumbs", typeof(T).Name);

                var fileExtension = Path.GetExtension(file.Name);
                var attachmentFileName = uniqueFileName + fileExtension;
                var thumbnailFileName = uniqueFileName + "_thumb.jpg";

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
                        await GenerateThumbnailAsync(fullFilePath, thumbsDir, thumbnailFileName);
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

        public async Task<string> UploadAsync<T>(string localFile, string contentType) where T : class
        {
            try
            {
                var uniqueFileName = Guid.NewGuid().ToString();
                var attachmentDir = Path.Combine("appdata", "attachments", typeof(T).Name);
                var thumbsDir = Path.Combine("appdata", "thumbs", typeof(T).Name);

                var fileExtension = Path.GetExtension(localFile);
                var attachmentFileName = uniqueFileName + fileExtension;
                var thumbnailFileName = uniqueFileName + "_thumb.jpg";

                // Save file to disk or database as needed
                var fullFilePath = Path.Combine(attachmentDir, attachmentFileName);

                Directory.CreateDirectory(attachmentDir);

                File.Move(localFile, fullFilePath);

                // Generate Thumbnail if it's an image file
                if (IsImageFile(contentType))
                {
                    await GenerateThumbnailAsync(fullFilePath, thumbsDir, thumbnailFileName);
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

        private async Task GenerateThumbnailAsync(string sourceFile, string dir, string thumbnailFilename)
        {
            try
            {
                Directory.CreateDirectory(dir);

                var thumbnailFile = Path.Combine(dir, thumbnailFilename);

                using (var image = await Image.LoadAsync(sourceFile))
                {
                    // Calculate thumbnail dimensions while maintaining aspect ratio
                    var (thumbWidth, thumbHeight) = CalculateThumbnailDimensions(image.Width, image.Height, thumbnailDimentions[0], thumbnailDimentions[1]);

                    // Create thumbnail
                    image.Mutate(x => x.Resize(thumbWidth, thumbHeight));
                    
                    // Save as JPEG with good quality
                    await image.SaveAsJpegAsync(thumbnailFile, new JpegEncoder { Quality = 85 });
                }

                _logger.LogInformation("Thumbnail generated: {ThumbnailPath}", thumbnailFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for: {OriginalPath}", thumbnailFilename);
            }
        }

        private (int width, int height) CalculateThumbnailDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
                return (originalWidth, originalHeight);

            var ratioX = (double)maxWidth / originalWidth;
            var ratioY = (double)maxHeight / originalHeight;
            var ratio = Math.Min(ratioX, ratioY);

            return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
        }
    }
}
