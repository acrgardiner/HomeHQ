using Microsoft.AspNetCore.Components.Forms;
using projectaardvarkx2.Entities;

namespace projectaardvarkx2.FileStorage
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync<T>(IBrowserFile? file) where T : class;
        Task<bool> UploadAsync<T>(byte[] fileBytes, Attachment attachment) where T : class;
        Task<string> UploadAsync<T>(string localFile, string contentType) where T : class;
    }
}
