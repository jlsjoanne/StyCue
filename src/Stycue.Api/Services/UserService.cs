using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Follow;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Users;
using Stycue.Api.Entities;
using Stycue.Api.Enums;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

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
            IFollowService followService, ILogger<UserService> logger)
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
            if(ValidateUserId<CurrentUserResponse>(currentUserId) is { } userError)
            {
                return userError;
            }

            var user = await FindActiveUserAsync(currentUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<CurrentUserResponse>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            var response = _mapper.Map<CurrentUserResponse>(user);

            return ApiResponse<CurrentUserResponse>.SuccessResult(response, "取得目前登入使用者資料成功");
        }

        public async Task<ApiResponse<PublicUserProfileResponse>> GetPublicProfileAsync(
            int targetUserId,
            int? currentUserId,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PublicUserProfileResponse>(targetUserId) is { } userError)
            {
                return userError;
            }

            if(currentUserId.HasValue && ValidateUserId<PublicUserProfileResponse>(currentUserId.Value) is { } currentUserError)
            {
                return currentUserError;
            }

            var user = await FindUserForProfileAsync(targetUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<PublicUserProfileResponse>.FailResult(
                    "找不到指定的使用者", "TARGET_USER_NOT_FOUND");
            }

            var response = await BuildPublicProfileResponseAsync(user, currentUserId, cancellationToken);

            return ApiResponse<PublicUserProfileResponse>.SuccessResult(response, "取得公開使用者資料成功");
        }

        public async Task<ApiResponse<MyUserProfileResponse>> GetMyProfileAsync(
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<MyUserProfileResponse>(currentUserId) is { } userError)
            {
                return userError;
            }

            var user = await FindUserForProfileAsync(currentUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            var response = BuildMyProfileResponse(user);

            return ApiResponse<MyUserProfileResponse>.SuccessResult(response, "取得個人資料成功");
        }

        public async Task<ApiResponse<MyUserProfileResponse>> UpdateMyProfileAsync(
            int currentUserId,
            UpdateUserProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<MyUserProfileResponse>(currentUserId) is { } userError)
            {
                return userError;
            }

            if(request == null)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "請求內容不可為空", "REQUEST_REQUIRED");
            }

            if(request.NickName != null && string.IsNullOrWhiteSpace(request.NickName.Trim()))
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "暱稱不可為空", "NICKNAME_REQUIRED");
            }

            if(request.BirthDate.HasValue && request.BirthDate.Value.Date > DateTime.UtcNow.Date)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "生日不可晚於今天", "INVALID_BIRTH_DATE");
            }

            var user = await FindUserForProfileAsync(currentUserId, true, cancellationToken);

            if(user == null)
            {
                return ApiResponse<MyUserProfileResponse>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            if (request.AvatarImageId.HasValue && 
                await ValidateAvatarImageAsync(currentUserId, request.AvatarImageId.Value, cancellationToken) is { } avatarError)
            {
                return avatarError;
            }

            ApplyProfileUpdates(user, GetOrCreateProfile(user), request);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedUser = await FindUserForProfileAsync(currentUserId, false, cancellationToken);

            var response = BuildMyProfileResponse(updatedUser!);

            return ApiResponse<MyUserProfileResponse>.SuccessResult(response, "個人資訊更新成功");
        }

        public async Task<ApiResponse<PrivateUserInfoResponse>> GetMyPrivateInfoAsync(
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PrivateUserInfoResponse>(currentUserId) is { } userError)
            {
                return userError;
            }

            var user = await FindUserForProfileAsync(currentUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<PrivateUserInfoResponse>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            var response = BuildPrivateInfoResponse(user.Profile);

            return ApiResponse<PrivateUserInfoResponse>.SuccessResult(response, "取得個人隱私資料成功");
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetMyPostsAsync(
            int currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PagedResponse<HomepageItemResponse>>(currentUserId) is { } userError)
            {
                return userError;
            }

            (var page, var pageSize) = NormalizePaging(request);

            var user = await FindActiveUserAsync(currentUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            var query = _dbContext.Posts
                .Where(p => p.UserId == user.Id && p.DeletedAt == null);

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query.OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().AsSplitQuery()
                .Include(p => p.User).ThenInclude(u => u.AvatarImage)
                .Include(p => p.Images).ThenInclude(i => i.FashionMetadata)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes).Include(p => p.PostFavorites)
                .Include(p => p.Comments).ToListAsync(cancellationToken);

            var items = posts.Select(p => _homepageItemResponseBuilder.BuildPostItem(p, user.Id)).ToList();

            var response = BuildPagedResponse(items, page, pageSize, totalCount);

            return ApiResponse<PagedResponse<HomepageItemResponse>>
                .SuccessResult(response, "取得發表分享/提問貼文成功");
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetMyCommissionsAsync(
            int currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PagedResponse<HomepageItemResponse>>(currentUserId) is { } userError)
            {
                return userError;
            }

            (var page, var pageSize) = NormalizePaging(request);

            var user = await FindActiveUserAsync(currentUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            var query = _dbContext.Commissions
                .Where(c => c.UserId == user.Id);

            var totalCount = await query.CountAsync(cancellationToken);

            var commissions = await query.OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().AsSplitQuery()
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.CommissionLikes).Include(c => c.CommissionFavorites)
                .Include(c => c.Comments).ToListAsync(cancellationToken);

            var items = commissions
                .Select(c => _homepageItemResponseBuilder.BuildCommissionItem(c, user.Id)).ToList();

            var response = BuildPagedResponse(items, page, pageSize, totalCount);

            return ApiResponse<PagedResponse<HomepageItemResponse>>
                .SuccessResult(response, "取得發表委託文成功");
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetMySavedPostsAsync(
            int currentUserId,
            UserContentQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PagedResponse<HomepageItemResponse>>(currentUserId) is { } userError)
            {
                return userError;
            }

            request ??= new UserContentQueryRequest();

            var (page, pageSize) = NormalizePaging(request);

            var user = await FindActiveUserAsync(currentUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            if( !Enum.IsDefined(typeof(HomepageFilter), request.Filter))
            {
                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "不合法的內容篩選條件", "INVALID_FILTER");
            }

            var includeSharePosts = request.Filter is HomepageFilter.All or HomepageFilter.PostShare;
            var includeQuestionPosts = request.Filter is HomepageFilter.All or HomepageFilter.PostAsk;
            var includeCommissions = request.Filter is HomepageFilter.All or HomepageFilter.Commission;

            var savedItems = new List<(HomepageItemResponse Item, DateTime SavedAt)>();

            if(includeSharePosts || includeQuestionPosts)
            {
                var postFavoriteQuery = _dbContext.PostFavorites
                    .AsNoTracking().AsSplitQuery()
                    .Where(f => f.UserId == currentUserId && f.Post.DeletedAt == null);

                if(!includeSharePosts)
                {
                    postFavoriteQuery = postFavoriteQuery.Where(p => p.Post.PostType != PostType.Share);
                }

                if (!includeQuestionPosts)
                {
                    postFavoriteQuery = postFavoriteQuery.Where(p => p.Post.PostType != PostType.Question);
                }

                var postFavorites = await postFavoriteQuery
                    .Include(f => f.Post.User).ThenInclude(u => u.AvatarImage)
                    .Include(f => f.Post.Images).ThenInclude(i => i.FashionMetadata)
                    .Include(f => f.Post.PostTags).ThenInclude(pt => pt.Tag)
                    .Include(f => f.Post.PostLikes).Include(p => p.Post.PostFavorites)
                    .Include(f => f.Post.Comments).ToListAsync(cancellationToken);

                savedItems.AddRange(postFavorites.Select(f => (
                    Item: _homepageItemResponseBuilder.BuildPostItem(f.Post, currentUserId),
                    SavedAt: f.CreatedAt)));
            }

            if (includeCommissions)
            {
                var commissionFavorites = await _dbContext.CommissionFavorites
                    .AsNoTracking().AsSplitQuery()
                    .Where(f => f.UserId == currentUserId)
                    .Include(f => f.Commission.User).ThenInclude(u => u.AvatarImage)
                    .Include(f => f.Commission.Images).ThenInclude(i => i.FashionMetadata)
                    .Include(f => f.Commission.CommissionTags).ThenInclude(ct => ct.Tag)
                    .Include(f => f.Commission.CommissionLikes).Include(f => f.Commission.CommissionFavorites)
                    .Include(f => f.Commission.Comments).ToListAsync(cancellationToken);

                savedItems.AddRange(commissionFavorites.Select(f => (
                    Item: _homepageItemResponseBuilder.BuildCommissionItem(f.Commission, currentUserId),
                    SavedAt: f.CreatedAt)));
            }

            var totalCount = savedItems.Count;

            var items = savedItems.OrderByDescending(x => x.SavedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => x.Item).ToList();

            var authorIds = items.Select(x => x.Author.UserId).Distinct().ToList();

            var followedUserIds = await _followService
                .GetFollowedUserIdsAsync(currentUserId, authorIds, cancellationToken);

            foreach(var item in items)
            {
                item.Author.IsFollowing = item.Author.UserId == currentUserId 
                    ? null : followedUserIds.Contains(item.Author.UserId);
            }

            var response = BuildPagedResponse(items, page, pageSize, totalCount);

            return ApiResponse<PagedResponse<HomepageItemResponse>>.SuccessResult(response, "取得收藏文章成功");
        }

        public async Task<ApiResponse<PagedResponse<FollowUserResponse>>> GetMyFollowingAsync(
            int currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PagedResponse<FollowUserResponse>>(currentUserId) is { } userError)
            {
                return userError;
            }

            (var page, var pageSize) = NormalizePaging(request);

            var user = await FindActiveUserAsync(currentUserId, false, cancellationToken);

            if( user == null)
            {
                return ApiResponse<PagedResponse<FollowUserResponse>>.FailResult(
                    "找不到目前登入使用者", "USER_NOT_FOUND");
            }

            // 找當前登入使用者追蹤的帳號
            var query = _dbContext.UserFollows.AsNoTracking()
                .Where(f => f.FollowerUserId == currentUserId)
                .Where(f => f.FollowingUser.DeactivatedAt == null);

            var totalCount = await query.CountAsync(cancellationToken);

            // 當前登入使用者追蹤清單分頁
            var follows = await query.OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Include(f => f.FollowingUser).ThenInclude(u => u.AvatarImage)
                .ToListAsync(cancellationToken);

            // 查有沒有互追
            var targetUserIds = follows.Select(f => f.FollowingUserId).ToList();

            var followedUserIds = await _followService
                .GetFollowedUserIdsAsync(currentUserId, targetUserIds, cancellationToken);

            var items = follows.Select(f => BuildFollowUserResponse(
                f.FollowingUser, f.CreatedAt, currentUserId, followedUserIds)).ToList();

            var response = BuildPagedResponse(items, page, pageSize, totalCount);

            return ApiResponse<PagedResponse<FollowUserResponse>>.SuccessResult(response, "取得追蹤中列表成功");
        }

        public async Task<ApiResponse<PagedResponse<FollowUserResponse>>> GetFollowersAsync(
            int targetUserId,
            int? currentUserId,
            PagedQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PagedResponse<FollowUserResponse>>(targetUserId) is { } userError)
            {
                return userError;
            }

            if(currentUserId.HasValue && 
                ValidateUserId<PagedResponse<FollowUserResponse>>(currentUserId.Value) is { } currentUserError)
            {
                return currentUserError;
            }

            (var page, var pageSize) = NormalizePaging(request);

            var user = await FindActiveUserAsync(targetUserId, false, cancellationToken);

            if(user == null)
            {
                return ApiResponse<PagedResponse<FollowUserResponse>>.FailResult(
                    "找不到指定的使用者", "TARGET_USER_NOT_FOUND");
            }

            var query = _dbContext.UserFollows.AsNoTracking()
                .Where(f => f.FollowingUserId == targetUserId)
                .Where(f => f.FollowerUser.DeactivatedAt == null);

            var totalCount = await query.CountAsync(cancellationToken);

            var follows = await query.OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Include(f => f.FollowerUser).ThenInclude(u => u.AvatarImage)
                .ToListAsync(cancellationToken);

            var followerUserIds = follows.Select(f => f.FollowerUserId).ToList();

            var followedUserIds = await _followService
                .GetFollowedUserIdsAsync(currentUserId, followerUserIds, cancellationToken);

            var items = follows.Select(f => BuildFollowUserResponse(
                f.FollowerUser, f.CreatedAt, currentUserId, followedUserIds)).ToList();

            var response = BuildPagedResponse(items, page, pageSize, totalCount);

            return ApiResponse<PagedResponse<FollowUserResponse>>.SuccessResult(response, "取得粉絲列表成功");

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
            var userUpdated = false;
            var profileUpdated = false;

            if(request.NickName != null)
            {
                user.NickName = request.NickName.Trim();
                userUpdated = true;
            }

            if (request.AvatarImageId.HasValue)
            {
                user.AvatarImageId = request.AvatarImageId.Value;
                userUpdated = true;
            }

            if(request.Bio != null)
            {
                var bio = request.Bio.Trim();
                profile.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;
                profileUpdated = true;
            }

            if (request.Gender.HasValue)
            {
                profile.Gender = request.Gender.Value;
                profileUpdated = true;
            }

            if (request.Height.HasValue)
            {
                profile.Height = request.Height.Value;
                profileUpdated = true;
            }

            if (request.Weight.HasValue)
            {
                profile.Weight = request.Weight.Value;
                profileUpdated = true;
            }

            if (request.BirthDate.HasValue)
            {
                profile.BirthDate = request.BirthDate.Value.Date;
                profileUpdated = true;
            }

            if (userUpdated)
            {
                user.UpdatedAt = now;
            }

            if (profileUpdated)
            {
                profile.UpdatedAt = now;
            }
        }

        // 四個列表 API 共用
        private static PagedResponse<T> BuildPagedResponse<T>(IReadOnlyList<T> items,
            int page, int pageSize, int totalCount)
        {
            return new PagedResponse<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double) pageSize)
            };
        }


        private FollowUserResponse BuildFollowUserResponse(
            User user, DateTime followedAt, int? currentUserId, ISet<int> followedUserIds)
        {
            return new FollowUserResponse
            {
                User = _userSummaryResponseBuilder.Build(user, currentUserId, followedUserIds),
                FollowedAt = followedAt
            };
        }
    }
}
