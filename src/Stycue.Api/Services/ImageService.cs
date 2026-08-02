using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Enums;
using Stycue.Api.Services.Models;
using Stycue.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stycue.Api.Options;

namespace Stycue.Api.Services
{
    public class ImageService : IImageService
    {
        private readonly AppDbContext _dbContext;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<ImageService> _logger;
        private readonly IOptions<ImageUploadOptions> _imageUploadOptions;

        private static readonly HashSet<string> AllowedFileExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };

        public ImageService(AppDbContext dbContext, IBlobStorageService blobStorageService,
            ILogger<ImageService> logger, IOptions<ImageUploadOptions> imageUploadOptions)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
            _logger = logger;
            _imageUploadOptions = imageUploadOptions;
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

        public async Task<ApiResponse<ImageResponse>> UploadAvatarImageAsync(
            int userId, UploadAvatarImageRequest request, CancellationToken cancellationToken = default)
        {
            if( request == null)
            {
                return ApiResponse<ImageResponse>.FailResult(
                    "請提供圖片上傳資料", "INVALID_IMAGE_REQUEST");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeactivatedAt == null, cancellationToken);

            if( user == null)
            {
                return ApiResponse<ImageResponse>.FailResult(
                    "查無此使用者", "USER_NOT_FOUND");
            }

            var oldAvatarImageId = user.AvatarImageId;

            var uploadRequest = new UploadImageRequest
            {
                File = request.File
            };

            var uploadResult = await UploadAsync(user.Id, uploadRequest, ImagePurpose.Profile, "avatars", cancellationToken);

            if( !uploadResult.Success || uploadResult.Data == null)
            {
                return uploadResult;
            }

            // check if there is old avatar image
            ImageAsset? oldAvatar = null;

            if (oldAvatarImageId.HasValue)
            {
                oldAvatar = await _dbContext.ImageAssets.FirstOrDefaultAsync(
                    image => image.Id == oldAvatarImageId.Value && image.OwnerUserId == user.Id &&
                        image.Purpose == ImagePurpose.Profile && image.DeletedAt == null, cancellationToken);
            }


            var now = DateTime.UtcNow;
            // update avatar image to user profile
            user.AvatarImageId = uploadResult.Data.ImageId;
            user.UpdatedAt = now;

            if( oldAvatar != null)
            {
                oldAvatar.DeletedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            if( oldAvatar != null)
            {
                try
                {
                    await _blobStorageService.DeleteIfExistsAsync(oldAvatar.BlobName, cancellationToken);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Old avatar metadata was soft deleted but blob deletion failed. ImageId: {ImageId}", oldAvatar.Id);
                }
            }

            return ApiResponse<ImageResponse>.SuccessResult(uploadResult.Data, "大頭貼上傳成功");
        }

        public Task<ApiResponse<ImageResponse>> UploadPostImageAsync(
            int userId, UploadImageRequest request, CancellationToken cancellationToken = default)
        {
            return UploadAsync(userId, request, ImagePurpose.Post, "posts", cancellationToken);
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

            var avatarUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.AvatarImageId == image.Id, cancellationToken);

            if( avatarUser != null)
            {
                avatarUser.AvatarImageId = null;
                avatarUser.UpdatedAt = DateTime.UtcNow;
            }

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
                    return ApiResponse<List<ImageAsset>>.FailResult("沒有權限使用部分圖片", "IMAGE_NOT_OWNER");
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

        public async Task<ApiResponse<List<ImageAsset>>> ValidateUpdatableImagesAsync(
            int userId, IEnumerable<int> imageIds, ImagePurpose purpose,
            int? currentPostId = null, int? currentCommentId = null, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<List<ImageAsset>>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            if (!Enum.IsDefined(typeof(ImagePurpose), purpose))
            {
                return ApiResponse<List<ImageAsset>>.FailResult("不合法的圖片用途", "INVALID_IMAGE_PURPOSE");
            }

            if( currentPostId.HasValue == currentCommentId.HasValue)
            {
                return ApiResponse<List<ImageAsset>>.FailResult(
                    "更新圖片目標不合法", "INVALID_IMAGE_UPDATE_TARGET");
            }

            if(currentPostId.HasValue && currentPostId.Value <= 0)
            {
                return ApiResponse<List<ImageAsset>>.FailResult(
                    "不合法的貼文 ID", "INVALID_POST_ID");
            }

            if(currentCommentId.HasValue && currentCommentId.Value <= 0)
            {
                return ApiResponse<List<ImageAsset>>.FailResult(
                    "不合法的留言 ID", "INVALID_COMMENT_ID");
            }

            var distinctImageIds = imageIds?.Distinct().ToList() ?? new List<int>();

            if(!distinctImageIds.Any())
            {
                return ApiResponse<List<ImageAsset>>.SuccessResult(
                    new List<ImageAsset>(), "圖片驗證成功");
            }

            if( distinctImageIds.Any(id => id <= 0))
            {
                return ApiResponse<List<ImageAsset>>.FailResult(
                    "包含不合法的圖片 ID", "INVALID_IMAGE_IDS");
            }

            // get images
            var images = await _dbContext.ImageAssets.Where(i => distinctImageIds.Contains(i.Id))
                .ToListAsync(cancellationToken);

            if( images.Count != distinctImageIds.Count)
            {
                return ApiResponse<List<ImageAsset>>.FailResult(
                    "包含不存在的圖片 ID", "IMAGE_NOT_FOUND");
            }

            foreach(var image in images)
            {
                if(image.OwnerUserId != userId)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult(
                        "沒有權限使用部分圖片", "IMAGE_NOT_OWNER");
                }

                if( image.Purpose != purpose)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult(
                        "圖片用途不符合", "INVALID_IMAGE_PURPOSE");
                }

                if( image.DeletedAt != null)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult(
                        "包含已刪除的圖片", "IMAGE_DELETED");
                }

                var unbound =
                    image.PostId == null &&
                    image.CommissionId == null &&
                    image.CommissionRepostId == null &&
                    image.CommentId == null;

                var boundToCurrentTarget =
                    (currentPostId.HasValue && image.PostId == currentPostId.Value) ||
                    (currentCommentId.HasValue && image.CommentId == currentCommentId.Value);

                if( !unbound && !boundToCurrentTarget)
                {
                    return ApiResponse<List<ImageAsset>>.FailResult(
                        "包含已被其他內容使用的圖片", "IMAGE_ALREADY_BOUND");
                }
            }

            return ApiResponse<List<ImageAsset>>.SuccessResult(images, "圖片驗證成功");
        }

        // private method
        // 共用圖片上傳方法
        private async Task<ApiResponse<ImageResponse>> UploadAsync(
            int userId, UploadImageRequest request, ImagePurpose purpose, string folder, CancellationToken cancellationToken)
        {
            // Validate user
            if( userId <= 0)
            {
                return ApiResponse<ImageResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

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

            var todayStartUtc = now.Date;
            var tomorrowStartUtc = todayStartUtc.AddDays(1);

            var todayImages = await _dbContext.ImageAssets.AsNoTracking()
                .Where(i => i.OwnerUserId == userId && i.CreatedAt >= todayStartUtc && i.CreatedAt < tomorrowStartUtc)
                .ToListAsync(cancellationToken);

            var currentCount = todayImages.Count;
            var currentTotalBytes = todayImages.Sum(i => i.FileSize);

            var options = _imageUploadOptions.Value;

            if(currentCount >= options.DailyMaxImageCount)
            {
                return ApiResponse<ImageResponse>.FailResult("今日圖片上傳數量已達上限", "IMAGE_DAILY_COUNT_LIMIT_EXCEEDED");
            }
            if(currentTotalBytes + request.File.Length > options.DailyMaxTotalBytes)
            {
                return ApiResponse<ImageResponse>.FailResult("今日圖片上傳容量已達上限", "IMAGE_DAILY_SIZE_LIMIT_EXCEEDED");
            }

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

            var readUrl = _blobStorageService.GenerateReadUrl(image.BlobName);

            return ApiResponse<ImageResponse>.SuccessResult(new ImageResponse
            {
                ImageId = image.Id,
                Purpose = image.Purpose,
                Url = readUrl,
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
