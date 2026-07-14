using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;
using Stycue.Api.Entities;
using Stycue.Api.Enums;

namespace Stycue.Api.Services.Interfaces
{
    public interface IImageService
    {
        Task<ApiResponse<ImageResponse>> UploadCommissionImageAsync(
            int userId, UploadImageRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<ImageResponse>> UploadCommentImageAsync(
            int userId, UploadImageRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<ImageResponse>> UploadPostImageAsync(
            int userId, UploadImageRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<ImageResponse>> UploadAvatarImageAsync(
            int userId, UploadAvatarImageRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<object>> DeleteAsync(
            int userId, int imageId, CancellationToken cancellationToken =default);

        // Use for validate Image in Create Post/Commission/Comment
        Task<ApiResponse<List<ImageAsset>>> ValidateBindableImagesAsync(
            int userId, IEnumerable<int> imageIds, ImagePurpose purpose, 
            CancellationToken cancellationToken = default);

        // Use for validate Image in Update Post/ Comment
        Task<ApiResponse<List<ImageAsset>>> ValidateUpdatableImagesAsync(
            int userId, IEnumerable<int> imageIds, ImagePurpose purpose,
            int? currentPostId = null, int? currentCommentId = null, CancellationToken cancellationToken = default);
    }
}
