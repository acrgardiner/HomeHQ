using projectaardvarkx2.Repositories;
using projectaardvarkx2.Entities;
using Microsoft.AspNetCore.Components.Forms;
using static MudBlazor.CategoryTypes;

namespace projectaardvarkx2.FileStorage
{
    public class FileStorageService : IFileStorageService
    {
        private long maxFileSize = 1024 * 1024 * 15;

        private readonly IGenericRepository<Attachment> _attachmentRepository;

        public FileStorageService(IGenericRepository<Attachment> attachmentRepository)
        {
            _attachmentRepository = attachmentRepository;
        }

        public async Task<(byte[], string)> GetImageBytesAsync(Guid attachmentId)
        {
            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);

            if (attachment == null)
            {
                throw new FileNotFoundException($"Attachment with ID {attachmentId} not found.");
            }

            var filePath = Path.Combine("appdata", "uploads", attachment.LocalFileName);

            return (File.ReadAllBytes(filePath), attachment.ContentType);
        }

        public async Task<string> UploadAsync<T>(IBrowserFile? file) where T : class
        {
            var fileExtension = Path.GetExtension(file.Name);
            var localFileName = Path.Combine(typeof(T).Name, Guid.NewGuid().ToString() + fileExtension);

            // Save file to disk or database as needed
            var filePath = Path.Combine("appdata", "uploads", localFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.OpenReadStream(maxFileSize).CopyToAsync(stream);
            }

            return localFileName;
        }
    }
}
