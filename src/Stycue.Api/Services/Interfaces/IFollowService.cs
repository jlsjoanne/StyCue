using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Follow;

namespace Stycue.Api.Services.Interfaces
{
    public interface IFollowService
    {
        Task<ApiResponse<FollowResponse>> FollowUserAsync(
            int userId, int targetUserId, CancellationToken cancellationToken = default);

        Task<ApiResponse<FollowResponse>> UnfollowUserAsync(
            int userId, int targetUserId, CancellationToken cancellationToken = default);

        Task<bool?> IsFollowingAsync(
            int? currentUserId, int targetUserId, CancellationToken cancellationToken = default);

        Task<HashSet<int>> GetFollowedUserIdsAsync(
            int? currentUserId, IEnumerable<int> targetUserIds, CancellationToken cancellationToken = default);
    }
}
