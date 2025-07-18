using Microsoft.AspNetCore.StaticFiles;
using projectaardvarkx2.Entities;
using projectaardvarkx2.FileStorage;

namespace projectaardvarkx2.Services
{
    public interface IAssetImportService
    {
        Task<int> ImportAssets();
    }

    public class AssetImportService : IAssetImportService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IEntityService<Asset> _assetService;
        private readonly IEntityService<Attachment> _attachmentService;

        public AssetImportService(
            IFileStorageService fileStorageService,
            IEntityService<Asset> assetService,
            IEntityService<Attachment> attachmentService)
        {
            _fileStorageService = fileStorageService;
            _assetService = assetService;
            _attachmentService = attachmentService;
        }

        public async Task<int> ImportAssets()
        {
            //Get all files from /imports
            const string DefaultContentType = "application/octet-stream";
            var provider = new FileExtensionContentTypeProvider();

            var importFiles = Directory.GetFiles(Path.Combine("appdata", "imports"));

            foreach (var importFile in importFiles)
            {
                var asset = new Asset
                {
                    Name = Path.GetFileNameWithoutExtension(importFile)
                };

                var savedAsset = await _assetService.AddAsync(asset);

                var safeFileName = Path.Combine(typeof(Asset).Name, Guid.NewGuid().ToString() + Path.GetExtension(importFile));

                if (!provider.TryGetContentType(importFile, out string contentType))
                {
                    contentType = DefaultContentType;
                }

                var attachment = new Attachment
                {
                    Name = "",
                    ParentId = savedAsset.Id,
                    LocalFileName = safeFileName,
                    OriginFileName = Path.GetFileName(importFile),
                    ContentType = contentType,
                    Extension = Path.GetExtension(importFile),
                    FileSize = new FileInfo(importFile).Length
                };

                await _attachmentService.AddAsync(attachment);

                //Move file
                File.Move(importFile, Path.Combine("appdata", "uploads", safeFileName));
            }

            return importFiles.Length;
        }
    }
}
