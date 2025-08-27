using Microsoft.AspNetCore.Components.Forms;
using projectaardvarkx2.Entities;

namespace projectaardvarkx2.FileStorage
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync<T>(IBrowserFile? file) where T : class;
        Task<(string, float)> UploadAsync<T>(byte[] fileBytes, string sourceFilename, string contentType) where T : class;
        Task<(string, float)> ReUploadAsync<T>(Attachment attachment, byte[] fileBytes) where T : class;
        Task<string> UploadAsync<T>(string localFile, string contentType) where T : class;
    }
}
