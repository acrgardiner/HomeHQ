using Microsoft.AspNetCore.StaticFiles;
using projectaardvarkx2.Entities;
using projectaardvarkx2.FileStorage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using Image = SixLabors.ImageSharp.Image;


namespace projectaardvarkx2.Services
{
    public interface IThumbnailService
    {
        Task<int> RebuildAll();
        Task GenerateThumbnailAsync<T>(string sourceFile);
    }

    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;

        private readonly IEntityService<Asset> _assetService;
        private readonly IEntityService<Attachment> _attachmentService;

        private readonly int[] thumbnailDimentions = [1200, 1200]; //[width, height]
        private readonly int thumbnailJpegQuality = 80;

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
                    if (attachment != null && IsImageFile(attachment.ContentType))
                    {
                        string attachmentFile = Path.Combine(assetAttachmentDir, attachment.LocalFileName);
                        await GenerateThumbnailAsync<Asset>(attachmentFile);
                        processedCount++;
                    }
                }
            }

            return processedCount;
        }

        public async Task GenerateThumbnailAsync<T>(string sourceFile)
        {
            try
            {
                var thumbsDir = Path.Combine("appdata", "thumbs", typeof(T).Name);
                var thumbnailFilename = Path.GetFileNameWithoutExtension(sourceFile) + "_thumb.jpg";

                Directory.CreateDirectory(thumbsDir);

                var thumbnailFile = Path.Combine(thumbsDir, thumbnailFilename);

                using (var image = await Image.LoadAsync(sourceFile))
                {
                    // Calculate thumbnail dimensions while maintaining aspect ratio
                    var (thumbWidth, thumbHeight) = CalculateThumbnailDimensions(image.Width, image.Height, thumbnailDimentions[0], thumbnailDimentions[1]);

                    // Create thumbnail
                    image.Mutate(x => x.Resize(thumbWidth, thumbHeight));

                    await image.SaveAsJpegAsync(thumbnailFile, new JpegEncoder { Quality = thumbnailJpegQuality });
                }

                _logger.LogInformation("Thumbnail generated: {ThumbnailPath}", thumbnailFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for: {0}", sourceFile);
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

        private bool IsImageFile(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var imageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
            return imageTypes.Contains(contentType.ToLower());
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
}
