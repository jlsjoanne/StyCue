using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Enums;
using Stycue.Api.Services.Models;
using Stycue.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Stycue.Api.Services
{
    public class ImageService : IImageService
    {
        private readonly AppDbContext _dbContext;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<ImageService> _logger;

        private static readonly HashSet<string> AllowedFileExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };

        public ImageService(AppDbContext dbContext, IBlobStorageService blobStorageService, ILogger<ImageService> logger)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public Task<ApiResponse<ImageResponse>> UploadCommissionImageAsync(
            int userId, UploadImageRequest request, CancellationToken cancellationToken = default)
        {
            return UploadAsync(userId, request, ImagePurpose.Commission, "commissions", cancellationToken);
        }

        public Task<ApiResponse<ImageResponse>> UploadCommentImageAsync(
            int userId, UploadImageRequest request, CancellationToken cancellationToken = default)
        {
            return UploadAsync(userId, request, ImagePurpose.Comment, "comments", cancellationToken);
        }

        public async Task<ApiResponse<object>> DeleteAsync(
            int userId, int imageId, CancellationToken cancellationToken = default)
        {
            var image = await _dbContext.ImageAssets.FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken);

            if(image == null)
            {
                return ApiResponse<object>.FailResult("圖片不存在", "IMAGE_NOT_FOUND");
            }

            if(userId != image.OwnerUserId)
            {
                return ApiResponse<object>.FailResult("沒有權限刪除此圖片", "FORBIDDEN");
            }

            if(image.DeletedAt != null)
            {
                return ApiResponse<object>.FailResult("圖片已刪除", "IMAGE_ALREADY_DELETED");
            }

            // ImageAsset soft delete => metadata紀錄刪除時間
            image.DeletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 從Blob Storage把圖片檔案刪除
            try
            {
                await _blobStorageService.DeleteIfExistsAsync(image.BlobName, cancellationToken);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Image metadata soft deleted but blob delete failed. ImageId: {ImageId}, BlobName: {BlobName}", 
                    image.Id, image.BlobName);
            }

            return ApiResponse<object>.SuccessResult(null!, "圖片刪除成功");
        }

        public async Task<ApiResponse<List<ImageAsset>>> ValidateBindableImagesAsync(
            int userId, IEnumerable<int> imageIds, ImagePurpose purpose, CancellationToken cancellationToken = default)
        {
            if( userId <= 0)
            {
                return ApiResponse<List<ImageAsset>>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            if( !Enum.IsDefined(typeof(ImagePurpose), purpose))
            {
                return ApiResponse<List<ImageAsset>>.FailResult("不合法的圖片用途", "INVALID_IMAGE_PURPOSE");
            }

            var distinctImageIds = imageIds?.Distinct().ToList() ?? new List<int>();

            if(!distinctImageIds.Any())
            {
                return ApiResponse<List<ImageAsset>>.SuccessResult(new List<ImageAsset>(), "圖片驗證成功");
            }

            if(distinctImageIds.Any(id => id <= 0))
            {
                return ApiResponse<List<ImageAsset>>.FailResult("包含不合法的圖片 ID", "INVALID_IMAGE_IDS");
            }

            var images = await _dbContext.ImageAssets.Where(image => distinctImageIds.Contains(image.Id))
                .ToListAsync(cancellationToken);

            if(images.Count != distinctImageIds.Count)
            {
                return ApiResponse<List<ImageAsset>>.FailResult("包含不存在的圖片 ID", "IMAGE_NOT_FOUND");
            }

            foreach(var image in images)
            {
                if(image.OwnerUserId != userId)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult("沒有權限使用部分圖片", "INVALID_IMAGE_PURPOSE");
                }

                if(image.Purpose != purpose)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult("圖片用途不符合", "INVALID_IMAGE_PURPOSE");
                }

                if( image.DeletedAt != null)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult("包含已刪除的圖片", "IMAGE_DELETED");
                }

                var alreadyBound = image.PostId != null ||
                    image.CommissionId != null ||
                    image.CommissionRepostId != null ||
                    image.CommentId != null;

                if (alreadyBound)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult("包含已被使用的圖片", "IMAGE_ALREADY_BOUND");
                }
            }

            return ApiResponse<List<ImageAsset>>.SuccessResult(images, "圖片驗證成功");
        }

        // private method
        // 共用圖片上傳方法
        private async Task<ApiResponse<ImageResponse>> UploadAsync(
            int userId, UploadImageRequest request, ImagePurpose purpose, string folder, CancellationToken cancellationToken)
        {
            if( request == null)
            {
                return ApiResponse<ImageResponse>.FailResult("請提供圖片上傳資料", "INVALID_IMAGE_REQUEST");
            }
            
            var validateFileError = ValidateImageFile(request.File);

            if(validateFileError != null)
            {
                return validateFileError;
            }

            var now = DateTime.UtcNow;
            var extension = Path.GetExtension(request.File.FileName);
            var blobName = $"{folder}/{userId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";

            // Upload to Blob Storage Operation
            BlobUploadResult uploadResult;

            await using var stream = request.File.OpenReadStream();

            uploadResult = await _blobStorageService.UploadAsync(stream,blobName,request.File.ContentType, cancellationToken);


            // Create ImageAsset
            var image = new ImageAsset
            {
                Url = uploadResult.Url,
                BlobName = uploadResult.BlobName,
                ContainerName = uploadResult.ContainerName,
                ContentType = uploadResult.ContentType,
                FileSize = request.File.Length,
                OwnerUserId = userId,
                Purpose = purpose,
                CreatedAt = now
            };

            // Check if request has Category or Brand, if yes => create FashionMetadata
            var brand = String.IsNullOrWhiteSpace(request.Brand) ? null : request.Brand.Trim();

            if(request.Category != null || brand != null)
            {
                image.FashionMetadata = new ImageFashionMetadata
                {
                    Category = request.Category,
                    Brand = brand
                };
            }

            // Add to database
            _dbContext.ImageAssets.Add(image);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStorageService.DeleteIfExistsAsync(blobName, CancellationToken.None);
                throw;
            }

            var sasUrl = _blobStorageService.GenerateReadSasUrl(image.BlobName);

            return ApiResponse<ImageResponse>.SuccessResult(new ImageResponse
            {
                ImageId = image.Id,
                Purpose = image.Purpose.ToString(),
                Url = sasUrl,
                Category = image.FashionMetadata?.Category,
                Brand = image.FashionMetadata?.Brand
            }, "圖片上傳成功");
        }

        // 驗證圖片檔案大小、類型
        private static ApiResponse<ImageResponse>? ValidateImageFile(IFormFile? file)
        {
            const long maxFileSize = 10 * 1024 * 1024;
            

            // Check if there is a file
            if(file == null || file.Length == 0)
            {
                return ApiResponse<ImageResponse>.FailResult("請選擇要上傳的圖片。", "IMAGE_FILE_REQUIRED");
            }
            
            // Check if it is a image file
            if(String.IsNullOrWhiteSpace(file.ContentType) ||  !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<ImageResponse>.FailResult("只允許上傳圖片檔案。", "INVALID_IMAGE_CONTENT_TYPE");
            }

            var extensions = Path.GetExtension(file.FileName).ToLowerInvariant();

            if( !AllowedFileExtensions.Contains(extensions))
            {
                return ApiResponse<ImageResponse>.FailResult("只允許上傳圖片檔案。", "INVALID_IMAGE_EXTENSIONS");
            }

            // Check file size not larger than maximum size
            if(file.Length > maxFileSize)
            {
                return ApiResponse<ImageResponse>.FailResult("圖片大小不可超過 10MB。", "IMAGE_FILE_TOO_LARGE");
            }

            return null;
        }
        
    }
}
