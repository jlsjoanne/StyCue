using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Comments;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Enums;
using Stycue.Api.Data;
using AutoMapper;

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
            // place holder
            return ApiResponse<List<CommentResponse>>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<CommentResponse>> CreateForCommissionAsync(
            int userId, int commissionId, CreateCommentRequest request, CancellationToken cancellationToken = default)
        {
            // place holder
            return ApiResponse<CommentResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<List<CommentResponse>>> GetPostCommentsAsync(
            int? userId, int postId, CancellationToken cancellationToken = default)
        {
            // place holder
            return ApiResponse<List<CommentResponse>>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<CommentResponse>> CreateForPostAsync(
            int userId, int postId, CreateCommentRequest request, CancellationToken cancellationToken = default)
        {
            // place holder
            return ApiResponse<CommentResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<CommentResponse>> ReplyAsync(
            int userId, int parentCommentId, CreateCommentRequest request, CancellationToken cancellationToken = default)
        {
            // place holder
            return ApiResponse<CommentResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<CommentResponse>> UpdateAsync(
            int userId, int commentId, UpdateCommentRequest request, CancellationToken cancellationToken = default)
        {
            // place holder
            return ApiResponse<CommentResponse>.FailResult("place holder", "PLACE_HOLDER");
        }

        public async Task<ApiResponse<object>> DeleteAsync(
            int userId, int commentId, CancellationToken cancellationToken)
        {
            // place holder
            return ApiResponse<object>.FailResult("place holder", "PLACE_HOLDER");
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
                .Include(c => c.User)
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

            var response = comments.Select(c => BuildCommentResponse(c, currentUserId)).ToList();

            return ApiResponse<List<CommentResponse>>.SuccessResult(response, "取得留言列表成功");
        }

        // 建立根留言、驗證圖片、綁圖片、儲存、查單筆 response
        // CreateForCommissionAsync / CreateForPostAsync共用
        private async Task<ApiResponse<CommentResponse>> CreateRootCommentAsync(
            int userId, int? postId, int? commissionId, CreateCommentRequest request, CancellationToken cancellationToken)
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
        // 驗證圖片在 ImageService.ValidateBindableImagesAsync => 在CreateRootCommentAsync, ReplyAsync, UpdateAsync內呼叫
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
        private CommentResponse BuildCommentResponse(Comment comment, int? currentUserId)
        {
            var response = _mapper.Map<CommentResponse>(comment);

            response.IsOwner = currentUserId.HasValue && comment.UserId == currentUserId.Value;
            response.CanEdit = currentUserId.HasValue && CanEditComment(comment, currentUserId.Value);
            response.CanDelete = currentUserId.HasValue && CanDeleteComment(comment, currentUserId.Value);
            response.LikeCount = comment.CommentLikes.Count;
            response.IsLiked = currentUserId.HasValue &&
                comment.CommentLikes.Any(like => like.UserId == currentUserId.Value);
            response.Images = _imageResponseBuilder.BuildList(comment.Images);
            response.Replies = comment.Replies.Where(r => r.DeletedAt == null)
                .OrderBy(r => r.CreatedAt)
                .Select(r => BuildCommentResponse(r, currentUserId))
                .ToList();

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
