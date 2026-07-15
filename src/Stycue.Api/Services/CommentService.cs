using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Comments;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Enums;
using Stycue.Api.Data;
using AutoMapper;
using Stycue.Api.Extensions;

namespace Stycue.Api.Services
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _dbContext;
        private readonly IImageService _imageService;
        private readonly IImageResponseBuilder _imageResponseBuilder;
        private readonly IMapper _mapper;

        public CommentService(AppDbContext dbContext, IImageService imageService, IImageResponseBuilder imageResponseBuilder, IMapper mapper)
        {
            _dbContext = dbContext;
            _imageService = imageService;
            _imageResponseBuilder = imageResponseBuilder;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<CommentResponse>>> GetCommissionCommentsAsync(
            int? userId, int commissionId, CancellationToken cancellationToken = default)
        {
            if( commissionId <= 0)
            {
                return ApiResponse<List<CommentResponse>>.FailResult(
                    "委託文識別碼不合法", "INVALID_COMMISSION_ID");
            }

            var commission= await _dbContext.Commissions
                    .AsNoTracking().FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);

            if(commission == null)
            {
                return ApiResponse<List<CommentResponse>>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            var isClosed = commission.Status == CommissionStatus.Closed || commission.ClosedAt != null;
            var isOwner = OwnershipGuard.IsOwner(commission.UserId, userId);

            if( isClosed && !isOwner)
            {
                return ApiResponse<List<CommentResponse>>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            return await GetCommentsAsync(
                userId, postId: null, commissionId: commissionId, cancellationToken);
        }

        public async Task<ApiResponse<CommentResponse>> CreateForCommissionAsync(
            int userId, int commissionId, UpsertCommentRequest request, CancellationToken cancellationToken = default)
        {
            if( userId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的使用者 ID", "INVALID_USER_ID");
            }

            if( commissionId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的委託文 ID", "INVALID_COMMISSION_ID");
            }

            if(request == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "請提供留言資料", "INVALID_COMMENT_REQUEST");
            }

            var commission = await _dbContext.Commissions
                .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);

            if( commission == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            if( commission.Status == CommissionStatus.Closed || commission.ClosedAt != null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            if( commission.UserId == userId)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "委託建立者不可留言自己的委託", "COMMISSION_OWNER_CANNOT_COMMENT");
            }

            // 歷史委託仍可討論，因此不因委託狀態或到期時間阻擋新增留言
            // 保留作為若未來此規則有更動可直接開啟
            // -----------------------------------------
            //var now = DateTime.UtcNow;
            //if( commission.ExpiredAt <= now)
            //{
            //    return ApiResponse<CommentResponse>.FailResult(
            //        "委託已到期，無法新增留言", "COMMISSION_EXPIRED");
            //}

            //if (commission.Status == CommissionStatus.Closed ||
            //    commission.Status == CommissionStatus.Rewarded ||
            //    commission.Status == CommissionStatus.NoAward ||
            //    commission.ClosedAt != null ||
            //    commission.AwardedCommentId != null ||
            //    commission.AwardedAt != null || commission.RewardSettledAt != null)
            //{
            //    return ApiResponse<CommentResponse>.FailResult(
            //        "目前委託狀態無法新增留言", "COMMISSION_CANNOT_COMMENT");
            //}

            // ----------------------------------------------------

            return await CreateRootCommentAsync(userId, postId: null, commissionId: commission.Id,
                request, cancellationToken);
        }

        public async Task<ApiResponse<List<CommentResponse>>> GetPostCommentsAsync(
            int? userId, int postId, CancellationToken cancellationToken = default)
        {
            if (postId <= 0)
            {
                return ApiResponse<List<CommentResponse>>.FailResult(
                    "貼文識別碼不合法", "INVALID_POST_ID");
            }

            var postExist = await _dbContext.Posts
                    .AsNoTracking().AnyAsync(p => p.Id == postId && p.DeletedAt == null, cancellationToken);

            if (!postExist)
            {
                return ApiResponse<List<CommentResponse>>.FailResult(
                    "找不到指定的貼文", "POST_NOT_FOUND");
            }

            return await GetCommentsAsync(
                userId, postId, commissionId: null, cancellationToken);
        }

        public async Task<ApiResponse<CommentResponse>> CreateForPostAsync(
            int userId, int postId, UpsertCommentRequest request, CancellationToken cancellationToken = default)
        {
            if(userId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的使用者 ID", "INVALID_USER_ID");
            }

            if(postId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的貼文ID", "INVALID_POST_ID");
            }

            var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.DeletedAt == null, cancellationToken);

            if (post == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "找不到指定的貼文", "POST_NOT_FOUND");
            }

            return await CreateRootCommentAsync(userId, post.Id, null, request, cancellationToken);
        }

        public async Task<ApiResponse<CommentResponse>> ReplyAsync(
            int userId, int parentCommentId, UpsertCommentRequest request, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的使用者 ID", "INVALID_USER_ID");
            }

            if( parentCommentId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的留言 ID", "INVALID_COMMENT_ID");
            }

            if( request == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "請提供留言資料", "INVALID_COMMENT_REQUEST");
            }

            var parentComment = await FindActiveCommentAsync(parentCommentId, cancellationToken);

            if( parentComment == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "找不到指定的留言", "COMMENT_NOT_FOUND");
            }

            if( parentComment.ParentCommentId != null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不可回覆留言回覆", "REPLY_TO_REPLY_NOT_ALLOWED");
            }

            if( parentComment.PostId.HasValue == parentComment.CommissionId.HasValue)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言目標不合法", "INVALID_COMMENT_TARGET");
            }

            if(parentComment.PostId.HasValue)
            {
                var postExists = await _dbContext.Posts.AsNoTracking()
                    .AnyAsync(p => p.Id == parentComment.PostId.Value && p.DeletedAt == null, cancellationToken);

                if(!postExists)
                {
                    return ApiResponse<CommentResponse>.FailResult(
                        "找不到指定的貼文", "POST_NOT_FOUND");
                }
            }

            if(parentComment.CommissionId.HasValue)
            {
                var commissionExists = await _dbContext.Commissions.AsNoTracking()
                    .AnyAsync(c => c.Id == parentComment.CommissionId.Value &&
                        c.Status != CommissionStatus.Closed && c.ClosedAt == null, cancellationToken);

                if(!commissionExists)
                {
                    return ApiResponse<CommentResponse>.FailResult(
                        "找不到指定的委託文", "COMMISSION_NOT_FOUND");
                }
            }

            var content = NormalizeContent(request.Content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言內容不可為空", "COMMENT_CONTENT_REQUIRED");
            }

            var imageResult = await _imageService.ValidateBindableImagesAsync(
                userId, request.ImageIds, ImagePurpose.Comment, cancellationToken);

            if(!imageResult.Success)
            {
                return ApiResponse<CommentResponse>.FailResult(imageResult.Message, imageResult.ErrorCode);
            }

            var reply = new Comment
            {
                UserId = userId,
                PostId = parentComment.PostId,
                CommissionId = parentComment.CommissionId,
                ParentCommentId = parentComment.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Comments.Add(reply);

            SetCommentImages(reply, imageResult.Data ?? [], replaceExisting: false);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var replyForResponse = await FindCommentForResponseAsync(reply.Id, cancellationToken);

            if( replyForResponse == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言建立後查詢失敗", "COMMENT_RESPONSE_NOT_FOUND");
            }

            return ApiResponse<CommentResponse>.SuccessResult(
                BuildCommentResponse(replyForResponse, userId), "留言建立成功");
        }

        public async Task<ApiResponse<CommentResponse>> UpdateAsync(
            int userId, int commentId, UpsertCommentRequest request, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的使用者 ID", "INVALID_USER_ID");
            }

            if(commentId <= 0)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "不合法的留言 ID", "INVALID_COMMENT_ID");
            }

            if( request == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "請提供留言資料", "INVALID_COMMENT_REQUEST");
            }

            var comment = await FindActiveCommentAsync(commentId, cancellationToken);

            if( comment == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "找不到指定的留言", "COMMENT_NOT_FOUND");
            }

            if( !CanEditComment(comment, userId))
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "只有留言作者可以編輯留言", "COMMENT_NOT_OWNER");
            }

            var content = NormalizeContent(request.Content);

            if(string.IsNullOrWhiteSpace(content))
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言內容不可為空", "COMMENT_CONTENT_REQUIRED");
            }

            var imageResult = await _imageService.ValidateUpdatableImagesAsync(
                userId, request.ImageIds, ImagePurpose.Comment, currentPostId: null, currentCommentId: comment.Id, cancellationToken);

            if(!imageResult.Success)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    imageResult.Message, imageResult.ErrorCode);
            }

            comment.Content = content;
            comment.UpdatedAt = DateTime.UtcNow;

            SetCommentImages(comment, imageResult.Data ?? [], replaceExisting: true);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var commentForResponse = await FindCommentForResponseAsync(comment.Id, cancellationToken);

            if( commentForResponse == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言更新後查詢失敗", "COMMENT_RESPONSE_NOT_FOUND");
            }

            return ApiResponse<CommentResponse>.SuccessResult(
                BuildCommentResponse(commentForResponse, userId), "留言更新成功");
        }

        public async Task<ApiResponse<object>> DeleteAsync(
            int userId, int commentId, CancellationToken cancellationToken)
        {
            if (userId <= 0)
            {
                return ApiResponse<object>.FailResult(
                    "不合法的使用者 ID", "INVALID_USER_ID");
            }

            if (commentId <= 0)
            {
                return ApiResponse<object>.FailResult(
                    "不合法的留言 ID", "INVALID_COMMENT_ID");
            }

            var comment = await FindActiveCommentAsync(commentId, cancellationToken);

            if( comment == null)
            {
                return ApiResponse<object>.FailResult(
                    "找不到指定的留言", "COMMENT_NOT_FOUND");
            }

            if( !CanDeleteComment(comment, userId))
            {
                return ApiResponse<object>.FailResult(
                    "只有留言作者可以刪除留言", "COMMENT_NOT_OWNER");
            }

            comment.DeletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse<object>.SuccessResult(new { commentId = comment.Id }, "留言已刪除");
        }




        // private helper

        // 留言列表查詢的共用 base query
        // 要 Include Comment, Reply
        private IQueryable<Comment> BuildCommentQuery(int? postId, int? commissionId)
        {
            var query = _dbContext.Comments.AsNoTracking().AsSplitQuery()
                .Where(c => c.DeletedAt == null && c.ParentCommentId == null);

            if (postId.HasValue)
            {
                query = query.Where(c => c.PostId == postId.Value);
            }

            if (commissionId.HasValue)
            {
                query = query.Where(c => c.CommissionId == commissionId.Value);
            }

            return query
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommentLikes)
                .Include(c => c.Replies.Where(r => r.DeletedAt == null)).ThenInclude(r => r.User)
                .Include(c => c.Replies.Where(r => r.DeletedAt == null)).ThenInclude(r => r.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.Replies.Where(r => r.DeletedAt == null)).ThenInclude(r => r.CommentLikes)
                .OrderBy(comment => comment.CreatedAt);

        }

        // 負責執行列表查詢並轉成 List<CommentResponse>
        // GetCommissionCommentsAsync / GetPostCommentsAsync 共用
        private async Task<ApiResponse<List<CommentResponse>>> GetCommentsAsync(
            int? currentUserId, int? postId, int? commissionId, CancellationToken cancellationToken)
        {
            if(postId.HasValue == commissionId.HasValue)
            {
                return ApiResponse<List<CommentResponse>>.FailResult(
                    "留言目標不合法", "INVALID_COMMENT_TARGET");
            }

            var comments = await BuildCommentQuery(postId, commissionId).ToListAsync(cancellationToken);

            var response = comments.Select(c => BuildCommentResponse(c, currentUserId, includeReplies: true)).ToList();

            return ApiResponse<List<CommentResponse>>.SuccessResult(response, "取得留言列表成功");
        }

        // 建立根留言、驗證圖片、綁圖片、儲存、查單筆 response
        // CreateForCommissionAsync / CreateForPostAsync共用
        private async Task<ApiResponse<CommentResponse>> CreateRootCommentAsync(
            int userId, int? postId, int? commissionId, UpsertCommentRequest request, CancellationToken cancellationToken)
        {
            if(postId.HasValue == commissionId.HasValue)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言目標不合法", "INVALID_COMMENT_TARGET");
            }

            if( request == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "請提供留言資料", "INVALID_COMMENT_REQUEST");
            }

            var content = NormalizeContent(request.Content);

            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言內容不可為空", "COMMENT_CONTENT_REQUIRED");
            }

            var imageResult = await _imageService.ValidateBindableImagesAsync(
                userId, request.ImageIds, ImagePurpose.Comment, cancellationToken);

            if(!imageResult.Success)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    imageResult.Message, imageResult.ErrorCode);
            }

            var comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                CommissionId = commissionId,
                ParentCommentId = null,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Comments.Add(comment);

            SetCommentImages(comment, imageResult.Data ?? [], replaceExisting: false);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var commentForResponse = await FindCommentForResponseAsync(comment.Id, cancellationToken);

            if(commentForResponse == null)
            {
                return ApiResponse<CommentResponse>.FailResult(
                    "留言建立後查詢失敗", "COMMENT_RESPONSE_NOT_FOUND");
            }

            return ApiResponse<CommentResponse>.SuccessResult(
                BuildCommentResponse(commentForResponse, userId), "留言建立成功");
        }

        // 目標留言查詢與基本檢查
        // 找到未刪除留言 DeletedAt == null
        // 驗證/修改用
        private Task<Comment?> FindActiveCommentAsync(int commentId, CancellationToken cancellationToken)
        {
            return _dbContext.Comments
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.DeletedAt == null, cancellationToken);
        }

        // 回傳 DTO 用，要拿來組 CommentResponse 的單筆留言
        // CreateRootCommentAsync / ReplyAsync / UpdateAsync 儲存後查回單筆 response
        // 不用 include replies
        private Task<Comment?> FindCommentForResponseAsync(int commentId, CancellationToken cancellationToken)
        {
            return _dbContext.Comments
                .Include(c => c.User)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.DeletedAt == null, cancellationToken);
        }

        // create / reply / update 共用
        private static void SetCommentImages(
            Comment comment, IEnumerable<ImageAsset> images, bool replaceExisting)
        {
            if (replaceExisting)
            {
                foreach(var existingImage in comment.Images.ToList())
                {
                    existingImage.CommentId = null;
                    existingImage.Comment = null;
                }

                comment.Images.Clear();
            }

            foreach(var image in images)
            {
                image.Comment = comment;

                if( !comment.Images.Any(existingImages => existingImages.Id == image.Id))
                {
                    comment.Images.Add(image);
                }
            }
        }

        // AutoMapper 打底
        // 補 IsOwner / CanEdit / CanDelete / LikeCount / IsLiked / Images / Replies
        private CommentResponse BuildCommentResponse(Comment comment, int? currentUserId, bool includeReplies = false)
        {
            var response = _mapper.Map<CommentResponse>(comment);
            var likes = comment.CommentLikes ?? [];
            var images = comment.Images ?? [];

            response.IsOwner = currentUserId.HasValue && comment.UserId == currentUserId.Value;
            response.CanEdit = currentUserId.HasValue && CanEditComment(comment, currentUserId.Value);
            response.CanDelete = currentUserId.HasValue && CanDeleteComment(comment, currentUserId.Value);
            response.LikeCount = likes.Count;
            response.IsLiked = currentUserId.HasValue ? likes.Any(like => like.UserId == currentUserId.Value) : null;
            response.Images = _imageResponseBuilder.BuildList(images);


            response.Replies = includeReplies
                ? (comment.Replies ?? [])
                    .Where(r => r.DeletedAt == null)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => BuildCommentResponse(r, currentUserId, includeReplies: false))
                    .ToList()
                : [];

            return response;
        }

        // 權限判斷
        // 是否可編輯
        private static bool CanEditComment(Comment comment, int userId)
        {
            return comment.UserId == userId && comment.DeletedAt == null;
        }

        // 是否可刪除
        private static bool CanDeleteComment(Comment comment, int userId)
        {
            return comment.UserId == userId && comment.DeletedAt == null;
        }

        // 整理留言內容
        // 統一 Trim()，並避免空白留言
        private static string NormalizeContent(string content)
        {
            return content?.Trim() ?? string.Empty;
        }
    }
}
