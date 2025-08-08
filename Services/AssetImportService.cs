using Microsoft.AspNetCore.StaticFiles;
using projectaardvarkx2.Entities;
using projectaardvarkx2.FileStorage;
using static MudBlazor.CategoryTypes;

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
        private readonly IEntityService<AttachmentType> _attachmentTypeService;

        public AssetImportService(
            IFileStorageService fileStorageService,
            IEntityService<Asset> assetService,
            IEntityService<Attachment> attachmentService,
            IEntityService<AttachmentType> attachmentTypeService
        )
        {
            _fileStorageService = fileStorageService;
            _assetService = assetService;
            _attachmentService = attachmentService;
            _attachmentTypeService = attachmentTypeService;
        }

        public async Task<int> ImportAssets()
        {
            //Get all files from /imports
            const string DefaultContentType = "application/octet-stream";
            var defaultAttachmentType = (await _attachmentTypeService.GetAllAsync()).Where(x => x.Default).FirstOrDefault();
            var provider = new FileExtensionContentTypeProvider();

            var importFiles = Directory.GetFiles(Path.Combine("appdata", "imports"));

            foreach (var importFile in importFiles)
            {
                var asset = new Asset
                {
                    Name = Path.GetFileNameWithoutExtension(importFile)
                };

                var savedAsset = await _assetService.AddAsync(asset);

                var safeFileName = Guid.NewGuid().ToString() + Path.GetExtension(importFile);

                if (!provider.TryGetContentType(importFile, out string contentType))
                {
                    contentType = DefaultContentType;
                }

                var attachment = new Attachment
                {
                    ParentId = savedAsset.Id,
                    ParentType = typeof(Asset).Name,
                    LocalFileName = safeFileName,
                    OriginFileName = Path.GetFileName(importFile),
                    ContentType = contentType,
                    Extension = Path.GetExtension(importFile),
                    FileSize = new FileInfo(importFile).Length,
                    AttachmentTypeId = defaultAttachmentType?.Id ?? Guid.Empty
                };

                await _attachmentService.AddAsync(attachment);

                //Move file
                File.Move(importFile, Path.Combine("appdata", "attachments", typeof(Asset).Name, safeFileName));
            }

            return importFiles.Length;
        }
    }
}
