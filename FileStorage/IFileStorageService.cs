using Microsoft.AspNetCore.Components.Forms;
using projectaardvarkx2.Entities;

namespace projectaardvarkx2.FileStorage
{
    public interface IFileStorageService
    {
        Task<bool> UploadAsync<T>(byte[] fileBytes, Attachment attachment) where T : class;
        Task<bool> UploadAsync<T>(string localFile, Attachment attachment) where T : class;
    }
}
