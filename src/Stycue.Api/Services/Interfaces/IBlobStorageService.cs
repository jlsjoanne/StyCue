using Stycue.Api.Services.Models;

namespace Stycue.Api.Services.Interfaces
{
    public interface IBlobStorageService
    {
        // 上傳檔案到 Blob
        Task<BlobUploadResult> UploadAsync(
            Stream fileStream, 
            string blobName, 
            string contentType, 
            CancellationToken cancellationToken = default);

        // 刪除 Blob

        Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default);

        // 產生短效 SAS URL
        // 因為 container 是 private，前端不能直接讀， 所以後端要產生短效 read-only SAS URL
        // SAS URL 不存 DB，只在 response 時動態產生。
        string GenerateReadSasUrl(string blobName, TimeSpan? expiresIn = null);

    }
}
