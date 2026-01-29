using HomeHQ.Entities;

namespace HomeHQ.FileStorage
{
    public interface IFileStorageService
    {
        Task<bool> UploadAsync<T>(byte[] fileBytes, Attachment attachment) where T : class;
        Task<bool> UploadAsync<T>(string localFile, Attachment attachment) where T : class;
    }
}
