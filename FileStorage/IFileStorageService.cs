using Microsoft.AspNetCore.Components.Forms;
using projectaardvarkx2.Entities;

namespace projectaardvarkx2.FileStorage
{
    public interface IFileStorageService
    {
        //Task<Attachment?> UploadAsync<T>(IFormFile? file, int maxFileSize, string[] allowedExtensions) where T : class;
        Task<string> UploadAsync<T>(IBrowserFile? file) where T : class;
        Task<string> UploadAsync<T>(string localFile, string contentType) where T : class;

        //public void Remove(string? path);

        //Task<(byte[], string)> GetImageBytesAsync(Guid attachmentId);
        //Task<(byte[], string)> GetThumbnailBytesAsync(Guid attachmentId);
    }
}
