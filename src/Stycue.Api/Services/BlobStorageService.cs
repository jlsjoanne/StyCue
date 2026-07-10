using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Stycue.Api.Options;
using Stycue.Api.Services.Models;
using Stycue.Api.Services.Interfaces;


namespace Stycue.Api.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private static readonly TimeSpan DefaultSasLifetime = TimeSpan.FromMinutes(60);

        private readonly BlobStorageOptions _options;
        // BlobContainerClient = 指向一個資料夾 / container 的操作物件
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IOptions<BlobStorageOptions> options)
        {
            _options = options.Value;

            if (String.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new InvalidOperationException("BlobStorage:ConnectionString is missing.");
            }
            if (String.IsNullOrWhiteSpace(_options.ContainerName))
            {
                throw new InvalidOperationException("BlobStorage:ContainerName is missing.");
            }

            _containerClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);

        }

        public async Task<BlobUploadResult> UploadAsync(
            Stream fileStream,
            string blobName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if(fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }
            if (String.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Blob name is required.", nameof(blobName));
            }
            if (String.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("Content type is required.", nameof(contentType));
            }

            // BlobClient = 指向 container 裡某個檔案 / blob 的操作物件
            // GetBlobClient(blobName) 本身不會真的去 Azure 查這個 blob 是否存在。
            // 它只是建立一個指向該 blob 路徑的 client 物件。
            var blobClient = _containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

            return new BlobUploadResult
            {
                BlobName = blobName,
                ContainerName = _options.ContainerName,
                Url = blobClient.Uri.ToString(),
                ContentType = contentType
            };
        }

        public async Task DeleteIfExistsAsync(
            string blobName,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Blob name is required.", nameof(blobName));
            }

            var blobClient = _containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync
                (DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken: cancellationToken);
        }

        public string GenerateReadSasUrl(string blobName,TimeSpan? expiresIn = null)
        {
            if (String.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Blob name is required.", nameof(blobName));
            }

            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Blob client cannot generate SAS URI. Use a connection string with account key.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = blobName,
                Resource = "b", // 指定 SAS resource type，"b": blob
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn ?? DefaultSasLifetime),
                Protocol = SasProtocol.Https
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task<bool> CheckConnectionAsync()
        {
            if (String.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                return false;
            }
            if (String.IsNullOrWhiteSpace(_options.ContainerName))
            {
                return false;
            }

            var containerClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);

            return await containerClient.ExistsAsync();
        }
    }
}
