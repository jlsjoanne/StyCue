using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Favorites;

namespace Stycue.Api.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<ApiResponse<FavoriteResponse>> FavoritePostAsync(
            int userId, int postId, CancellationToken cancellationToken = default);

        Task<ApiResponse<FavoriteResponse>> UnfavoritePostAsync(
            int userId, int postId, CancellationToken cancellationToken = default);

        Task<ApiResponse<FavoriteResponse>> FavoriteCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken = default);

        Task<ApiResponse<FavoriteResponse>> UnfavoriteCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken = default);
    }
}
