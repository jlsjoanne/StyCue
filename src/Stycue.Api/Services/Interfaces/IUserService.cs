using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Follow;
using Stycue.Api.DTOs.Users;

namespace Stycue.Api.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<CurrentUserResponse>> GetCurrentUserAsync(
              int currentUserId,
              CancellationToken cancellationToken = default);

        Task<ApiResponse<PublicUserProfileResponse>> GetPublicProfileAsync(
            int targetUserId,
            int? currentUserId,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<MyUserProfileResponse>> GetMyProfileAsync(
            int currentUserId,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<MyUserProfileResponse>> UpdateMyProfileAsync(
            int currentUserId,
            UpdateUserProfileRequest request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PrivateUserInfoResponse>> GetMyPrivateInfoAsync(
            int currentUserId,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetMyPostsAsync(
            int currentUserId,
            UserContentQueryRequest request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetMySavedPostsAsync(
            int currentUserId,
            UserContentQueryRequest request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PagedResponse<FollowUserResponse>>> GetMyFollowingAsync(
            int currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PagedResponse<FollowUserResponse>>> GetFollowersAsync(
            int targetUserId,
            int? currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default);
    }
}
