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

        Task<ApiResponse<object>> DeleteAsync(
            int userId, int imageId, CancellationToken cancellationToken =default);

        Task<ApiResponse<List<ImageAsset>>> ValidateBindableImagesAsync(
            int userId, IEnumerable<int> imageIds, ImagePurpose purpose, CancellationToken cancellationToken = default);
    }
}
