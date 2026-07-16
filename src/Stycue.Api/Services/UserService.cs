using Stycue.Api.Services.Interfaces;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Follow;
using Stycue.Api.DTOs.Users;
using Stycue.Api.Data;
using AutoMapper;
using Stycue.Api.Extensions;
using Stycue.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Enums;

namespace Stycue.Api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IUserSummaryResponseBuilder _userSummaryResponseBuilder;
        private readonly IHomepageItemResponseBuilder _homepageItemResponseBuilder;
        private readonly IFollowService _followService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            AppDbContext dbContext, IMapper mapper, 
            IUserSummaryResponseBuilder userSummaryResponseBuilder, 
            IHomepageItemResponseBuilder homepageItemResponseBuilder,
            IImageResponseBuilder imageResponseBuilder, IFollowService followService, ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _userSummaryResponseBuilder = userSummaryResponseBuilder;
            _homepageItemResponseBuilder = homepageItemResponseBuilder;
            _followService = followService;
            _logger = logger;
        }

        // Interface Public Methods
        public async Task<ApiResponse<CurrentUserResponse>> GetCurrentUserAsync(
              int currentUserId,
              CancellationToken cancellationToken = default)
        {
            return ApiResponse<CurrentUserResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<PublicUserProfileResponse>> GetPublicProfileAsync(
            int targetUserId,
            int? currentUserId,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<PublicUserProfileResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<MyUserProfileResponse>> GetMyProfileAsync(
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<MyUserProfileResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<MyUserProfileResponse>> UpdateMyProfileAsync(
            int currentUserId,
            UpdateUserProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<MyUserProfileResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<PrivateUserInfoResponse>> GetMyPrivateInfoAsync(
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<PrivateUserInfoResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetMyPostsAsync(
            int currentUserId,
            UserContentQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetMySavedPostsAsync(
            int currentUserId,
            UserContentQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<PagedResponse<FollowUserResponse>>> GetMyFollowingAsync(
            int currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<PagedResponse<FollowUserResponse>>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<PagedResponse<FollowUserResponse>>> GetFollowersAsync(
            int targetUserId,
            int? currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ApiResponse<PagedResponse<FollowUserResponse>>.FailResult("place holder", "PLACE_HOLDER");
        }

        // private methods

        private static ApiResponse<T>? ValidateUserId<T>(int userId)
        {
            return userId > 0 ? null : ApiResponse<T>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
        }

        private static (int page, int pageSize) NormalizePaging(PagedQueryRequest? request)
        {
            return PagingHelper.Normalize(
                request?.Page ?? PagingHelper.DefaultPage,
                request?.PageSize ?? PagingHelper.DefaultPageSize);
        }

        // 查 me、public profile、followers target 都要確認未停用
        private Task<User?> FindActiveUserAsync(
            int userId, bool needTracking, CancellationToken cancellationToken)
        {
            var user = _dbContext.Users.Where(u => u.Id == userId && u.DeactivatedAt == null);

            if(!needTracking)
            {
                user = user.AsNoTracking();
            }

            return user.FirstOrDefaultAsync(cancellationToken);
        }

        // profile 相關 API 需要 User + Profile + AvatarImage
        private Task<User?> FindUserForProfileAsync(
            int userId, bool needTracking, CancellationToken cancellationToken)
        {
            var user = _dbContext.Users.AsSplitQuery()
                .Where(u => u.Id == userId && u.DeactivatedAt == null);

            if(!needTracking)
            {
                user = user.AsNoTracking();
            }

            return user.Include(u => u.Profile).Include(u => u.AvatarImage).FirstOrDefaultAsync(cancellationToken);
        }

        // UpdateMyProfileAsync 避免 User.Profile == null
        private UserProfile GetOrCreateProfile(User user)
        {
            if(user.Profile != null)
            {
                return user.Profile;
            }

            var profile = new UserProfile
            {
                UserId = user.Id,
                User = user,
                CreatedAt = DateTime.UtcNow
            };

            user.Profile = profile;
            _dbContext.UserProfiles.Add(profile);

            return profile;
        }

        private MyUserProfileResponse BuildMyProfileResponse(User user)
        {
            var userSummary = _userSummaryResponseBuilder.Build(user);

            if(user.Profile == null)
            {
                return new MyUserProfileResponse
                {
                    User = userSummary
                };
            }

            var response = _mapper.Map<MyUserProfileResponse>(user.Profile);
            response.User = userSummary;
            return response;
        }

        private PrivateUserInfoResponse BuildPrivateInfoResponse(UserProfile? profile)
        {
            return profile == null ? new PrivateUserInfoResponse() : _mapper.Map<PrivateUserInfoResponse>(profile);
        }

        private async Task<PublicUserProfileResponse> BuildPublicProfileResponseAsync(
            User targetUser, int? currentUserId, CancellationToken cancellationToken)
        {
            var followerCount = await _dbContext.UserFollows.AsNoTracking()
                .CountAsync(f => f.FollowingUserId == targetUser.Id && f.FollowerUser.DeactivatedAt == null, cancellationToken);

            var followingCount = await _dbContext.UserFollows.AsNoTracking()
                .CountAsync(f => f.FollowerUserId == targetUser.Id && f.FollowingUser.DeactivatedAt == null, cancellationToken);

            var isFollowing = await _followService.IsFollowingAsync(currentUserId, targetUser.Id, cancellationToken);

            return new PublicUserProfileResponse
            {
                User = _userSummaryResponseBuilder.Build(targetUser),
                Bio = targetUser.Profile == null ? null : targetUser.Profile.Bio,
                FollowerCount = followerCount,
                FollowingCount = followingCount,
                IsFollowing = isFollowing
            };
        }

        // 驗證帳號大頭貼
        private async Task<ApiResponse<MyUserProfileResponse>?> ValidateAvatarImageAsync(
            int currentUserId, int? avatarImageId, CancellationToken cancellationToken)
        {
            if(!avatarImageId.HasValue)
            {
                return null;
            }

            var image = await _dbContext.ImageAssets.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == avatarImageId.Value, cancellationToken);

            if(image == null)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "找不到指定的大頭貼圖片","AVATAR_IMAGE_NOT_FOUND");
            }

            if(image.OwnerUserId != currentUserId)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "只能使用自己上傳的大頭貼圖片", "AVATAR_IMAGE_NOT_OWNER");
            }

            if(image.DeletedAt != null)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "無法使用已刪除的大頭貼圖片", "AVATAR_IMAGE_DELETED");
            }

            if(image.Purpose != ImagePurpose.Profile)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "圖片用途不符合大頭貼", "INVALID_AVATAR_IMAGE_PURPOSE");
            }

            return null;
        }

        private static void ApplyProfileUpdates(User user, UserProfile profile,
            UpdateUserProfileRequest request)
        {
            var now = DateTime.UtcNow;

            if (request.NickName != null)
            {
                user.NickName = request.NickName.Trim();
                user.UpdatedAt = now;
            }

            if (request.AvatarImageId.HasValue)
            {
                user.AvatarImageId = request.AvatarImageId.Value;
                user.UpdatedAt = now;
            }

            if (request.Bio != null)
            {
                profile.Bio = request.Bio.Trim();
                profile.UpdatedAt = now;
            }

            if (request.Gender.HasValue)
            {
                profile.Gender = request.Gender.Value;
                profile.UpdatedAt = now;
            }

            if (request.Height.HasValue)
            {
                profile.Height = request.Height.Value;
                profile.UpdatedAt = now;
            }

            if (request.Weight.HasValue)
            {
                profile.Weight = request.Weight.Value;
                profile.UpdatedAt = now;
            }

            if (request.BirthDate.HasValue)
            {
                profile.BirthDate = request.BirthDate.Value;
                profile.UpdatedAt = now;
            }
        }
    }
}
