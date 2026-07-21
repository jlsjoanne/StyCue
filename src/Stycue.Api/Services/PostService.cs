using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Posts;
using Stycue.Api.Entities;
using Stycue.Api.Enums;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Stycue.Api.DTOs.Tags;

namespace Stycue.Api.Services
{
    public class PostService : IPostService
    {

        private readonly AppDbContext _dbContext;
        private readonly ITagService _tagService;
        private readonly IImageService _imageService;
        private readonly IFollowService _followService;
        private readonly IImageResponseBuilder _imageResponseBuilder;
        private readonly IUserSummaryResponseBuilder _userSummaryResponseBuilder;
        private readonly IMapper _mapper;
        private readonly ILogger<PostService> _logger;
        private readonly ISearchDocumentProjector _searchDocumentProjector;


        public PostService(
            AppDbContext dbContext, ITagService tagService, IImageService imageService, IFollowService followService,
            IImageResponseBuilder imageResponseBuilder, 
            IUserSummaryResponseBuilder userSummaryResponseBuilder, 
            IMapper mapper, ILogger<PostService> logger,
            ISearchDocumentProjector searchDocumentProjector)
        {
            _dbContext = dbContext;
            _tagService = tagService;
            _imageService = imageService;
            _followService = followService;
            _imageResponseBuilder = imageResponseBuilder;
            _userSummaryResponseBuilder = userSummaryResponseBuilder;
            _mapper = mapper;
            _logger = logger;
            _searchDocumentProjector = searchDocumentProjector;
        }

        public async Task<ApiResponse<PostDetailResponse>> CreateAsync(
            int userId, PostRequest request, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PostDetailResponse>(userId) is { } userError)
            {
                return userError;
            }

            if(ValidatePostRequest(request, out var title, out var content,
                out string? outfitStyle, out string? outfitOccasion, out DateOnly? outfitDate, out string? outfitLocation) is { } requestError)
            {
                return requestError;
            }

            var imageResult = await _imageService.ValidateBindableImagesAsync(
                userId, request.ImageIds, ImagePurpose.Post, cancellationToken);

            if(!imageResult.Success)
            {
                return ApiResponse<PostDetailResponse>.FailResult(imageResult.Message, imageResult.ErrorCode);
            }

            var tagResult = await _tagService.ValidateTagIdsAsync(request.TagIds, cancellationToken);

            if(!tagResult.Success)
            {
                return ApiResponse<PostDetailResponse>.FailResult(tagResult.Message, tagResult.ErrorCode);
            }

            var post = new Post
            {
                UserId = userId,
                Title = title,
                Content = content,
                PostType = request.PostType,
                OutfitStyle = outfitStyle,
                OutfitOccasion = outfitOccasion,
                OutfitDate = outfitDate,
                OutfitLocation = outfitLocation,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Posts.Add(post);

            SetPostImages(post, imageResult.Data ?? [], replaceExisting: false);
            SetPostTags(post, tagResult.Tags, replaceExisting: false);

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _searchDocumentProjector.UpsertPostAsync(post.Id, cancellationToken);

            var detail = await FindPostForDetailAsync(post.Id, cancellationToken);
            
            if( detail == null)
            {
                _logger.LogError("Post was created but detail query returned null. PostId: {PostId}, UserId: {UserId}",
                    post.Id, userId);

                return ApiResponse<PostDetailResponse>.FailResult("貼文建立成功，但讀取詳情失敗",
                    "POST_DETAIL_NOT_FOUND_AFTER_CREATE");
            }

            var response = BuildPostDetailResponse(detail, userId);

            return ApiResponse<PostDetailResponse>.SuccessResult(response, "貼文建立成功");

        }

        public async Task<ApiResponse<PostDetailResponse>> GetPostAsync(
            int? userId, int postId, CancellationToken cancellationToken = default)
        {
            if(ValidatePostId<PostDetailResponse>(postId) is { } postError)
            {
                return postError;
            }

            var post = await FindPostForDetailAsync(postId, cancellationToken);

            if(post == null)
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "找不到指定的貼文", "POST_NOT_FOUND");
            }

            var response = BuildPostDetailResponse(post, userId);

            response.Author.IsFollowing = await _followService.IsFollowingAsync(
                userId, post.UserId, cancellationToken);

            return ApiResponse<PostDetailResponse>.SuccessResult(response, "取得貼文成功");
        }

        public async Task<ApiResponse<PostDetailResponse>> UpdateAsync(
            int userId, int postId, PostRequest request, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PostDetailResponse>(userId) is { } userError)
            {
                return userError;
            }

            if(ValidatePostId<PostDetailResponse>(postId) is { } postError)
            {
                return postError;
            }

            var post = await FindPostForUpdateAsync(postId, cancellationToken);

            if (post == null)
            {
                return ApiResponse<PostDetailResponse>.FailResult("找不到指定的貼文", "POST_NOT_FOUND");
            }

            if( !CanEditPost(post, OwnershipGuard.IsOwner(post.UserId, userId)))
            {
                return ApiResponse<PostDetailResponse>.FailResult("只有貼文作者可以編輯貼文", "POST_NOT_OWNER");
            }

            if (ValidatePostRequest(request, out var title, out var content,
                out string? outfitStyle, out string? outfitOccasion, out DateOnly? outfitDate, out string? outfitLocation) is { } requestError)
            {
                return requestError;
            }

            var imageResult = await _imageService.ValidateUpdatableImagesAsync(userId, request.ImageIds,
                ImagePurpose.Post, postId, null, cancellationToken);

            if(!imageResult.Success)
            {
                return ApiResponse<PostDetailResponse>.FailResult(imageResult.Message, imageResult.ErrorCode);
            }

            var tagResult = await _tagService.ValidateTagIdsAsync(request.TagIds, cancellationToken);

            if(!tagResult.Success)
            {
                return ApiResponse<PostDetailResponse>.FailResult(tagResult.Message, tagResult.ErrorCode);
            }

            post.Title = title;
            post.Content = content;
            post.PostType = request.PostType;
            post.OutfitStyle = outfitStyle;
            post.OutfitOccasion = outfitOccasion;
            post.OutfitDate = outfitDate;
            post.OutfitLocation = outfitLocation;
            post.UpdatedAt = DateTime.UtcNow;

            SetPostImages(post, imageResult.Data ?? [], replaceExisting: true);
            SetPostTags(post, tagResult.Tags ?? [], replaceExisting: true);

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _searchDocumentProjector.UpsertPostAsync(post.Id, cancellationToken);

            var detail = await FindPostForDetailAsync(post.Id, cancellationToken);

            if (detail == null)
            {
                _logger.LogError("Post was updated but detail query returned null. PostId: {PostId}, UserId: {UserId}",
                    post.Id, userId);

                return ApiResponse<PostDetailResponse>.FailResult("貼文更新成功，但讀取詳情失敗",
                    "POST_DETAIL_NOT_FOUND_AFTER_UPDATE");
            }

            var response = BuildPostDetailResponse(detail, userId);

            return ApiResponse<PostDetailResponse>.SuccessResult(response, "貼文更新成功");
        }

        public async Task<ApiResponse<PostDeleteResponse>> DeleteAsync(
            int userId, int postId, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<PostDeleteResponse>(userId) is { } userError)
            {
                return userError;
            }

            if(ValidatePostId<PostDeleteResponse>(postId) is { } postError)
            {
                return postError;
            }

            var post = await FindPostForUpdateAsync(postId, cancellationToken);

            if (post == null)
            {
                return ApiResponse<PostDeleteResponse>.FailResult("找不到指定的貼文", "POST_NOT_FOUND");
            }

            if( !CanDeletePost(post, OwnershipGuard.IsOwner(post.UserId, userId)))
            {
                return ApiResponse<PostDeleteResponse>.FailResult("只有貼文作者可以刪除貼文", "POST_NOT_OWNER");
            }

            post.UpdatedAt = DateTime.UtcNow;
            post.DeletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _searchDocumentProjector.HidePostAsync(post.Id, cancellationToken);

            var response = new PostDeleteResponse
            {
                PostId = post.Id,
                DeletedAt = post.DeletedAt.Value
            };

            return ApiResponse<PostDeleteResponse>.SuccessResult(response, "貼文刪除成功");
        }


        // private helper

        // validate userId
        private static ApiResponse<T>? ValidateUserId<T>(int userId)
        {
            return userId > 0
                  ? null
                  : ApiResponse<T>.FailResult(
                      "不合法的使用者 ID",
                      "INVALID_USER_ID");
        }

        // validate postId
        private static ApiResponse<T>? ValidatePostId<T>(int postId)
        {
            return postId > 0
                ? null
                : ApiResponse<T>.FailResult(
                    "不合法的貼文 ID",
                    "INVALID_POST_ID");
        }


        // if post can be edited
        private static bool CanEditPost(Post post, bool isOwner)
        {
            return isOwner && post.DeletedAt == null;
        }

        // if post can be deleted
        private static bool CanDeletePost(Post post, bool isOwner)
        {
            return isOwner && post.DeletedAt == null;
        }

        // VValidate and normalize post request
        private static ApiResponse<PostDetailResponse>? ValidatePostRequest(
            PostRequest? request, out string title, out string content, 
            out string? outfitStyle, out string? outfitOccasion, out DateOnly? outfitDate, out string? outfitLocation)
        {
            title = string.Empty;
            content = string.Empty;
            outfitStyle = null;
            outfitOccasion = null;
            outfitDate = null;
            outfitLocation = null;

            if(request == null)
            {
                return ApiResponse<PostDetailResponse>.FailResult("貼文資料不可為空", "INVALID_REQUEST");
            }

            title = request.Title?.Trim() ?? string.Empty;
            content = request.Content?.Trim() ?? string.Empty;
            outfitStyle = string.IsNullOrWhiteSpace(request.OutfitStyle) ? null : request.OutfitStyle.Trim();
            outfitOccasion = string.IsNullOrWhiteSpace(request.OutfitOccasion) ? null : request.OutfitOccasion.Trim();
            outfitDate = request.OutfitDate;
            outfitLocation = string.IsNullOrWhiteSpace(request.OutfitLocation) ? null : request.OutfitLocation.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "貼文標題不可為空", "POST_TITLE_REQUIRED");
            }

            if(title.Length > 100)
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "貼文標題不可超過 100 個字", "POST_TITLE_TOO_LONG");
            }

            if(string.IsNullOrWhiteSpace(content))
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "貼文內容不可為空", "POST_CONTENT_REQUIRED");
            }

            if(content.Length > 4000)
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "貼文內容不可超過 4000 個字", "POST_CONTENT_TOO_LONG");
            }

            if( !Enum.IsDefined(typeof(PostType), request.PostType))
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "貼文類型不合法", "INVALID_POST_TYPE");
            }

            if(outfitStyle?.Length > 50)
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "穿搭風格不可超過 50 個字", "OUTFIT_STYLE_TOO_LONG");
            }

            if(outfitOccasion?.Length > 50)
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "穿搭場合不可超過 50 個字", "OUTFIT_OCCASION_TOO_LONG");
            }
            
            if(outfitLocation?.Length > 100)
            {
                return ApiResponse<PostDetailResponse>.FailResult(
                    "穿搭地點不可超過 100 個字", "OUTFIT_LOCATION_TOO_LONG");
            }

            return null;
        }

        // Set Image to bind to Post
        private static void SetPostImages(Post post, IEnumerable<ImageAsset> images, bool replaceExisting)
        {
            if(replaceExisting)
            {
                foreach(var existingImage in post.Images.ToList())
                {
                    existingImage.PostId = null;
                    existingImage.Post = null;
                }

                // 清空 Post.Images navigation collection
                // ImageAsset 本身還在，只是解除 PostId
                post.Images.Clear();
            }

            foreach(var image in images)
            {
                image.Post = post;
                if( !post.Images.Any(existingImage => existingImage.Id == image.Id))
                {
                    post.Images.Add(image);
                }
            }
        }

        // set tag to bind to post
        private static void SetPostTags(Post post, IEnumerable<Tag> tags, bool replaceExisting)
        {
            var tagIds = tags.Select(tag => tag.Id).ToHashSet();

            if (replaceExisting)
            {
                foreach(var existingPostTags in post.PostTags.Where(pt => !tagIds.Contains(pt.TagId)).ToList())
                {
                    post.PostTags.Remove(existingPostTags);
                }
            }

            var existingTagIds = post.PostTags.Select(postTag => postTag.TagId).ToHashSet();

            foreach(var tag in tags)
            {
                if (existingTagIds.Contains(tag.Id))
                {
                    continue;
                }

                post.PostTags.Add(new PostTag
                {
                    Post = post,
                    TagId = tag.Id
                });

                existingTagIds.Add(tag.Id);
            }
        }

        private async Task<Post?> FindPostForUpdateAsync(int postId, CancellationToken cancellationToken)
        {
            return await _dbContext.Posts
                .Include(p => p.Images)
                .Include(p => p.PostTags)
                .FirstOrDefaultAsync(p => p.Id == postId && p.DeletedAt == null, cancellationToken);
        }

        private async Task<Post?> FindPostForDetailAsync(int postId, CancellationToken cancellationToken)
        {
            return await _dbContext.Posts.AsNoTracking().AsSplitQuery()
                .Include(p => p.User).ThenInclude(u => u.AvatarImage)
                .Include(p => p.Images).ThenInclude(i => i.FashionMetadata)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes)
                .Include(p => p.PostFavorites)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId && p.DeletedAt == null, cancellationToken);
        }

        private PostDetailResponse BuildPostDetailResponse(Post post, int? currentUserId)
        {
            var response = _mapper.Map<PostDetailResponse>(post);

            var isOwner = OwnershipGuard.IsOwner(post.UserId, currentUserId);

            response.Author = _userSummaryResponseBuilder.Build(post.User);
            response.IsOwner = isOwner;
            response.CanEdit = CanEditPost(post, isOwner);
            response.CanDelete = CanDeletePost(post, isOwner);
            response.LikeCount = post.PostLikes.Count;
            response.CommentCount = post.Comments.Count(c => c.DeletedAt == null);
            response.FavoriteCount = post.PostFavorites.Count;

            response.IsLiked = currentUserId.HasValue ? post.PostLikes.Any(like => like.UserId == currentUserId.Value) : null;
            response.IsFavorited = currentUserId.HasValue ? post.PostFavorites.Any(f => f.UserId == currentUserId.Value) : null;

            response.Images = _imageResponseBuilder.BuildList(
                post.Images.OrderBy(i => i.CreatedAt));

            response.Tags = post.PostTags
                .OrderBy(pt => pt.Tag.Name)
                .Select(pt => _mapper.Map<TagResponse>(pt.Tag))
                .ToList();

            return response;
        }
    }
}
