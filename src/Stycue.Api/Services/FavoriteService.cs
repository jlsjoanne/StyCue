using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Favorites;
using Stycue.Api.Entities;
using Stycue.Api.Enums;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Services
{
    public class FavoriteService : IFavoriteService
    {
        private const string CommissionTargetType = "commission";
        private const string PostTargetType = "post";

        private readonly AppDbContext _dbContext;

        public FavoriteService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<FavoriteResponse>> FavoritePostAsync(
            int userId, int postId, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if(ValidateTargetId(postId, "不合法的貼文 ID", "INVALID_POST_ID") is { } postError)
            {
                return postError;
            }

            var postExists = await PostExistsAsync(postId, cancellationToken);

            if (!postExists)
            {
                return ApiResponse<FavoriteResponse>.FailResult(
                    "找不到指定的貼文", "POST_NOT_FOUND");
            }

            var alreadyFavorited = await _dbContext.PostFavorites
                .AnyAsync(f => f.PostId == postId && f.UserId == userId, cancellationToken);

            if(!alreadyFavorited)
            {
                _dbContext.PostFavorites.Add(new PostFavorite
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var favoriteCount = await _dbContext.PostFavorites
                .CountAsync(f => f.PostId == postId, cancellationToken);

            var response = BuildResponse(PostTargetType, postId, true, favoriteCount);

            return ApiResponse<FavoriteResponse>.SuccessResult(response, "貼文收藏成功");
        }

        public async Task<ApiResponse<FavoriteResponse>> UnfavoritePostAsync(
            int userId, int postId, CancellationToken cancellationToken = default)
        {
            if (ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateTargetId(postId, "不合法的貼文 ID", "INVALID_POST_ID") is { } postError)
            {
                return postError;
            }

            var postExists = await PostExistsAsync(postId, cancellationToken);

            if (!postExists)
            {
                return ApiResponse<FavoriteResponse>.FailResult(
                    "找不到指定的貼文", "POST_NOT_FOUND");
            }

            var existingFavorite = await _dbContext.PostFavorites
                .FirstOrDefaultAsync(f => f.PostId == postId && f.UserId == userId, cancellationToken);

            if(existingFavorite != null)
            {
                _dbContext.PostFavorites.Remove(existingFavorite);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var favoriteCount = await _dbContext.PostFavorites
                .CountAsync(f => f.PostId == postId, cancellationToken);

            var response = BuildResponse(PostTargetType, postId, false, favoriteCount);

            return ApiResponse<FavoriteResponse>.SuccessResult(response, "貼文取消收藏成功");
        }

        public async Task<ApiResponse<FavoriteResponse>> FavoriteCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId(userId) is { } userError)
            {
                return userError;
            }
            
            if(ValidateTargetId(commissionId, "不合法的委託文 ID", "INVALID_COMMISSION_ID") is { } commissionError)
            {
                return commissionError;
            }

            var commissionExists = await CommissionExistsAsync(commissionId, cancellationToken);

            if(!commissionExists)
            {
                return ApiResponse<FavoriteResponse>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            var alreadyFavorited = await _dbContext.CommissionFavorites
                .AnyAsync(f => f.CommissionId == commissionId && f.UserId == userId, cancellationToken);

            if(!alreadyFavorited)
            {
                _dbContext.CommissionFavorites.Add(new CommissionFavorite
                {
                    CommissionId = commissionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var favoriteCount = await _dbContext.CommissionFavorites
                .CountAsync(f => f.CommissionId == commissionId, cancellationToken);

            var response = BuildResponse(CommissionTargetType, commissionId, true, favoriteCount);

            return ApiResponse<FavoriteResponse>.SuccessResult(response, "委託文收藏成功");
        }

        public async Task<ApiResponse<FavoriteResponse>> UnfavoriteCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken = default)
        {
            if (ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateTargetId(commissionId, "不合法的委託文 ID", "INVALID_COMMISSION_ID") is { } commissionError)
            {
                return commissionError;
            }

            var commissionExists = await CommissionExistsAsync(commissionId, cancellationToken);

            if (!commissionExists)
            {
                return ApiResponse<FavoriteResponse>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            var existingFavorite = await _dbContext.CommissionFavorites
                .FirstOrDefaultAsync(f => f.CommissionId == commissionId && f.UserId == userId, cancellationToken);

            if(existingFavorite != null)
            {
                _dbContext.CommissionFavorites.Remove(existingFavorite);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var favoriteCount = await _dbContext.CommissionFavorites
                .CountAsync(f => f.CommissionId == commissionId, cancellationToken);

            var response = BuildResponse(CommissionTargetType, commissionId, false, favoriteCount);

            return ApiResponse<FavoriteResponse>.SuccessResult(response, "委託文取消收藏成功");
        }


        // private methods

        // 建立收藏/取消收藏回應
        private static FavoriteResponse BuildResponse(
            string targetType, int targetId, bool isFavorited, int favoriteCount)
        {
            return new FavoriteResponse
            {
                TargetType = targetType,
                TargetId = targetId,
                IsFavorited = isFavorited,
                FavoriteCount = favoriteCount
            };
        }

        // 驗證private methods

        private static ApiResponse<FavoriteResponse>? ValidateUserId(int userId)
        {
            return userId > 0 ? null : ApiResponse<FavoriteResponse>.FailResult(
                "不合法的使用者 ID", "INVALID_USER_ID");
        }

        private static ApiResponse<FavoriteResponse>? ValidateTargetId(
            int targetId, string message, string errorCode)
        {
            return targetId > 0 ? null : ApiResponse<FavoriteResponse>.FailResult(message, errorCode);
        }

        private async Task<bool> CommissionExistsAsync(
            int commissionId, CancellationToken cancellationToken)
        {
            return await _dbContext.Commissions.AsNoTracking()
                .AnyAsync(c => c.Id == commissionId &&
                    c.Status != CommissionStatus.Closed && c.ClosedAt == null, cancellationToken);
        }

        private async Task<bool> PostExistsAsync(
            int postId, CancellationToken cancellationToken)
        {
            return await _dbContext.Posts.AsNoTracking()
                .AnyAsync(p => p.Id == postId && p.DeletedAt == null, cancellationToken);
        }
    }
}
