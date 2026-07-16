using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Follow;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;

namespace Stycue.Api.Services
{
    public class FollowService : IFollowService
    {
        private readonly AppDbContext _dbContext;

        public FollowService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<FollowResponse>> FollowUserAsync(
            int userId, int targetUserId, CancellationToken cancellationToken)
        {
            if(ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if(ValidateTargetId(targetUserId) is { } targetUserError)
            {
                return targetUserError;
            }

            if(ValidateNotSelfFollow(userId, targetUserId) is { } notSelfError)
            {
                return notSelfError;
            }

            var userExists = await ActiveUserExistsAsync(targetUserId, cancellationToken);

            if(!userExists)
            {
                return ApiResponse<FollowResponse>.FailResult("找不到指定的使用者", "TARGET_USER_NOT_FOUND");
            }

            var followExists = await _dbContext.UserFollows
                .AnyAsync(f => f.FollowerUserId == userId && f.FollowingUserId == targetUserId, cancellationToken);

            if (!followExists)
            {
                _dbContext.UserFollows.Add(new UserFollow
                {
                    FollowerUserId = userId,
                    FollowingUserId = targetUserId
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var followerCount = await GetFollowerCountAsync(targetUserId, cancellationToken);

            var response = BuildResponse(targetUserId, true, followerCount);

            return ApiResponse<FollowResponse>.SuccessResult(response, "追蹤使用者成功");
        }

        public async Task<ApiResponse<FollowResponse>> UnfollowUserAsync(
            int userId, int targetUserId, CancellationToken cancellationToken)
        {
            if (ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateTargetId(targetUserId) is { } targetUserError)
            {
                return targetUserError;
            }

            if (ValidateNotSelfFollow(userId, targetUserId) is { } notSelfError)
            {
                return notSelfError;
            }

            var userExists = await ActiveUserExistsAsync(targetUserId, cancellationToken);

            if (!userExists)
            {
                return ApiResponse<FollowResponse>.FailResult("找不到指定的使用者", "TARGET_USER_NOT_FOUND");
            }

            var existingFollow = await _dbContext.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerUserId == userId && f.FollowingUserId == targetUserId, cancellationToken);

            if(existingFollow != null)
            {
                _dbContext.UserFollows.Remove(existingFollow);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var followerCount = await GetFollowerCountAsync(targetUserId, cancellationToken);

            var response = BuildResponse(targetUserId, false, followerCount);

            return ApiResponse<FollowResponse>.SuccessResult(response, "取消追蹤使用者成功");
        }

        public async Task<bool?> IsFollowingAsync(
            int? currentUserId, int targetUserId, CancellationToken cancellationToken = default)
        {
            if(!currentUserId.HasValue)
            {
                return null;
            }

            if(currentUserId.Value == targetUserId)
            {
                return null;
            }

            return await _dbContext.UserFollows.AsNoTracking()
                .AnyAsync(f => f.FollowerUserId == currentUserId.Value && f.FollowingUserId == targetUserId, cancellationToken);
        }

        public async Task<HashSet<int>> GetFollowedUserIdsAsync(
            int? currentUserId, IEnumerable<int> targetUserIds, CancellationToken cancellationToken = default)
        {
            if(!currentUserId.HasValue)
            {
                return [];
            }

            var ids = targetUserIds.Where(id => id != currentUserId.Value)
                .Distinct().ToList();

            if(ids.Count == 0)
            {
                return [];
            }

            return await _dbContext.UserFollows.AsNoTracking()
                .Where(f => f.FollowerUserId == currentUserId.Value && ids.Contains(f.FollowingUserId))
                .Select(f => f.FollowingUserId).ToHashSetAsync(cancellationToken);
        }

        // private methods
        private static ApiResponse<FollowResponse>? ValidateUserId(int userId)
        {
            return userId > 0 ? null : ApiResponse<FollowResponse>.FailResult(
                "不合法的使用者 ID", "INVALID_USER_ID");
        }

        private static ApiResponse<FollowResponse>? ValidateTargetId(
            int targetId)
        {
            return targetId > 0 ? null : ApiResponse<FollowResponse>.FailResult(
                "不合法的使用者 ID", "INVALID_TARGET_USER_ID");
        }

        // 避免自己追蹤自己
        private static ApiResponse<FollowResponse>? ValidateNotSelfFollow(
            int userId, int targetUserId)
        {
            return userId != targetUserId ? null : ApiResponse<FollowResponse>.FailResult(
                "使用者不能追蹤自己", "CANNOT_FOLLOW_SELF");
        }

        // 確認目標 user 存在且未停用
        private async Task<bool> ActiveUserExistsAsync(int userId, CancellationToken cancellationToken)
        {
            return await _dbContext.Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.DeactivatedAt == null, cancellationToken);
        }

        // 計算目標使用者粉絲數
        private async Task<int> GetFollowerCountAsync(int targetUserId, CancellationToken cancellationToken)
        {
            return await _dbContext.UserFollows.AsNoTracking()
                .CountAsync(f => f.FollowingUserId == targetUserId, cancellationToken);
        }

        // 組回應
        private static FollowResponse BuildResponse(
            int targetUserId, bool isFollowing, int followerCount)
        {
            return new FollowResponse
            {
                TargetUserId = targetUserId,
                IsFollowing = isFollowing,
                FollowerCount = followerCount
            };
        }
    }
}
