using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Likes;
using Stycue.Api.Entities;
using Stycue.Api.Enums;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Services
{
    public class LikeService : ILikeService
    {
        private const string CommentTargetType = "comment";
        private const string CommissionTargetType = "commission";
        private const string PostTargetType = "post";

        private readonly AppDbContext _dbContext;

        public LikeService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        
        public async Task<ApiResponse<LikeResponse>> LikeCommentAsync(
            int userId, int commentId, CancellationToken cancellationToken)
        {
            if(ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if(ValidateTargetId(commentId, "不合法的留言 ID", "INVALID_COMMENT_ID") is { } commentIdError)
            {
                return commentIdError;
            }

            var comment = await FindActiveCommentAsync(commentId, cancellationToken);

            if(comment == null)
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的留言", "COMMENT_NOT_FOUND");
            }

            if( !await ActiveCommentTargetExistsAsync(comment, cancellationToken))
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的留言", "COMMENT_NOT_FOUND");
            }

            if(comment.CommissionId != null && comment.ParentCommentId == null &&
                comment.UserId == userId)
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "委託文留言者不可按讚自己的留言", "COMMISSION_COMMENTER_CANNOT_LIKE_OWN_COMMENT");
            }

            var alreadyLiked = await _dbContext.CommentLikes
                .AnyAsync(like => like.CommentId == commentId && like.UserId == userId, cancellationToken);

            if(!alreadyLiked)
            {
                _dbContext.CommentLikes.Add(new CommentLike
                {
                    CommentId = commentId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var likeCount = await _dbContext.CommentLikes
                .CountAsync(like => like.CommentId == commentId, cancellationToken);

            var response = BuildResponse(CommentTargetType, commentId, isLiked: true, likeCount);

            return ApiResponse<LikeResponse>.SuccessResult(response, "留言按讚成功");
        }

        public async Task<ApiResponse<LikeResponse>> UnlikeCommentAsync(
            int userId, int commentId, CancellationToken cancellationToken)
        {
            if (ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateTargetId(commentId, "不合法的留言 ID", "INVALID_COMMENT_ID") is { } commentIdError)
            {
                return commentIdError;
            }

            var comment = await FindActiveCommentAsync(commentId, cancellationToken);

            if (comment == null)
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的留言", "COMMENT_NOT_FOUND");
            }

            if (!await ActiveCommentTargetExistsAsync(comment, cancellationToken))
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的留言", "COMMENT_NOT_FOUND");
            }

            var existingLike = await _dbContext.CommentLikes
                .FirstOrDefaultAsync(like => like.UserId == userId && like.CommentId == commentId, cancellationToken);

            if(existingLike != null)
            {
                _dbContext.CommentLikes.Remove(existingLike);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var likeCount = await _dbContext.CommentLikes
                .CountAsync(like => like.CommentId == commentId, cancellationToken);

            var response = BuildResponse(CommentTargetType, commentId, false, likeCount);

            return ApiResponse<LikeResponse>.SuccessResult(response, "留言已取消按讚");
        }

        public async Task<ApiResponse<LikeResponse>> LikeCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken)
        {
            if (ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateTargetId(commissionId, "不合法的委託文 ID", "INVALID_COMMISSION_ID") is { } commissionIdError)
            {
                return commissionIdError;
            }

            var commissionExists = await CommissionExistsAsync(commissionId, cancellationToken);

            if (!commissionExists)
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            var alreadyLiked = await _dbContext.CommissionLikes
                .AnyAsync(like => like.CommissionId == commissionId && like.UserId == userId, cancellationToken);

            if (!alreadyLiked)
            {
                _dbContext.CommissionLikes.Add(new CommissionLike
                {
                    CommissionId = commissionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var likeCount = await _dbContext.CommissionLikes
                .CountAsync(like => like.CommissionId == commissionId, cancellationToken);

            var response = BuildResponse(CommissionTargetType, commissionId, isLiked: true, likeCount);

            return ApiResponse<LikeResponse>.SuccessResult(response, "委託文按讚成功");
        }

        public async Task<ApiResponse<LikeResponse>> UnlikeCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken)
        {
            if (ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateTargetId(commissionId, "不合法的委託文 ID", "INVALID_COMMISSION_ID") is { } commissionIdError)
            {
                return commissionIdError;
            }

            var commissionExists = await CommissionExistsAsync(commissionId, cancellationToken);
            if (!commissionExists)
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            var existingLike = await _dbContext.CommissionLikes
                .FirstOrDefaultAsync(like => like.UserId == userId && like.CommissionId == commissionId, cancellationToken);

            if (existingLike != null)
            {
                _dbContext.CommissionLikes.Remove(existingLike);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var likeCount = await _dbContext.CommissionLikes
                .CountAsync(like => like.CommissionId == commissionId, cancellationToken);

            var response = BuildResponse(CommissionTargetType, commissionId, false, likeCount);

            return ApiResponse<LikeResponse>.SuccessResult(response, "委託文已取消按讚");
        }

        public async Task<ApiResponse<LikeResponse>> LikePostAsync(
            int userId, int postId, CancellationToken cancellationToken)
        {
            if(ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if(ValidateTargetId(postId, "不合法的貼文 ID", "INVALID_POST_ID") is { } postError)
            {
                return postError;
            }

            var postExist = await PostExistsAsync(postId, cancellationToken);

            if (!postExist)
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的貼文", "POST_NOT_FOUND");
            }

            var alreadyLiked = await _dbContext.PostLikes
                .AnyAsync(like => like.PostId == postId && like.UserId == userId, cancellationToken);

            if(!alreadyLiked)
            {
                _dbContext.PostLikes.Add(new PostLike
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var likeCount = await _dbContext.PostLikes
                .CountAsync(like => like.PostId == postId, cancellationToken);

            var response = BuildResponse(PostTargetType, postId, isLiked: true, likeCount);

            return ApiResponse<LikeResponse>.SuccessResult(response, "貼文按讚成功");
        }

        public async Task<ApiResponse<LikeResponse>> UnlikePostAsync(
            int userId, int postId, CancellationToken cancellationToken)
        {
            if(ValidateUserId(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateTargetId(postId, "不合法的貼文 ID", "INVALID_POST_ID") is { } postError)
            {
                return postError;
            }

            var postExist = await PostExistsAsync(postId, cancellationToken);

            if (!postExist)
            {
                return ApiResponse<LikeResponse>.FailResult(
                    "找不到指定的貼文", "POST_NOT_FOUND");
            }

            var existingLike = await _dbContext.PostLikes
                .FirstOrDefaultAsync(like => like.PostId == postId && like.UserId == userId, cancellationToken);

            if (existingLike != null)
            {
                _dbContext.PostLikes.Remove(existingLike);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var likeCount = await _dbContext.PostLikes
                .CountAsync(like => like.PostId == postId, cancellationToken);

            var response = BuildResponse(PostTargetType, postId, false, likeCount);

            return ApiResponse<LikeResponse>.SuccessResult(response, "貼文已取消按讚");

        }


        // private helper

        // 建立按讚/取消按讚回應
        private static LikeResponse BuildResponse(
            string targetType, int targetId, bool isLiked, int likeCount)
        {
            return new LikeResponse
            {
                TargetType = targetType,
                TargetId = targetId,
                IsLiked = isLiked,
                LikeCount = likeCount
            };
        }

        // 驗證private methods

        private static ApiResponse<LikeResponse>? ValidateUserId(int userId)
        {
            return userId > 0 ? null : ApiResponse<LikeResponse>.FailResult(
                "不合法的使用者 ID", "INVALID_USER_ID");
        }

        private static ApiResponse<LikeResponse>? ValidateTargetId(
            int targetId, string message, string errorCode)
        {
            return targetId > 0 ? null : ApiResponse<LikeResponse>.FailResult(message, errorCode);
        }

        private async Task<Comment?> FindActiveCommentAsync(
            int commentId, CancellationToken cancellationToken)
        {
            return await _dbContext.Comments.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == commentId && c.DeletedAt == null, cancellationToken);
        }

        private async Task<bool> ActiveCommentTargetExistsAsync(
            Comment comment, CancellationToken cancellationToken)
        {
            if(comment.PostId.HasValue)
            {
                return await _dbContext.Posts.AsNoTracking()
                    .AnyAsync(p => p.Id == comment.PostId.Value && p.DeletedAt == null, cancellationToken);
            }

            if (comment.CommissionId.HasValue)
            {
                return await _dbContext.Commissions.AsNoTracking()
                    .AnyAsync(c => c.Id == comment.CommissionId.Value && c.Status != CommissionStatus.Closed &&
                        c.ClosedAt == null, cancellationToken);
            }

            return false;
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
