using AutoMapper;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.Enums;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Entities;
using Stycue.Api.DTOs.Tags;

namespace Stycue.Api.Services
{
    public class HomepageService : IHomepageService
    {
        private readonly AppDbContext _dbContext;
        private readonly IImageResponseBuilder _imageResponseBuilder;
        private readonly IUserSummaryResponseBuilder _userSummaryResponseBuilder;
        private readonly IMapper _mapper;
        private readonly ILogger<HomepageService> _logger;

        public HomepageService(AppDbContext dbContext, IImageResponseBuilder imageResponseBuilder, IUserSummaryResponseBuilder userSummaryResponseBuilder, IMapper mapper, ILogger<HomepageService> logger)
        {
            _dbContext = dbContext;
            _imageResponseBuilder = imageResponseBuilder;
            _userSummaryResponseBuilder = userSummaryResponseBuilder;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetHomepageAsync(
            int? userId, HomepageQueryRequest request, CancellationToken cancellationToken)
        {
            request ??= new HomepageQueryRequest();

            try
            {
                
                // validate userId
                if (userId.HasValue && ValidateUserId(userId.Value) is { } userError)
                {
                    return userError;
                }

                // normalize page and pageSize
                var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);

                // validate and normalize sortby and filter
                if(TryParseSortBy(request.SortBy, out var sortBy) is { } sortByError)
                {
                    return sortByError;
                }

                if(TryParseFilter(request.Filter, out var filter) is { } filterError)
                {
                    return filterError;
                }
              
                // 順序: build items by filter, then sort and page
                var response = await BuildHomepageResponseAsync(userId, filter, sortBy, page, pageSize, cancellationToken);

                return ApiResponse<PagedResponse<HomepageItemResponse>>.SuccessResult(
                    response, "首頁列表查詢成功");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Get homepage failed. UserId: {UserId}, SortBy: {SortBy}, Filter: {Filter}, Page: {Page}, PageSize: {PageSize}",
                    userId, request?.SortBy, request?.Filter, request?.Page, request?.PageSize);

                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "取得首頁列表失敗，請稍後再試", "HOMEPAGE_QUERY_FAILED");
            }
        }

        // private helpers

        // validate Id
        private static ApiResponse<PagedResponse<HomepageItemResponse>>? ValidateUserId(int userId)
        {
            return userId > 0 ? null : ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                "不合法的使用者 ID", "INVALID_USER_ID");
        }

        // validate sort by
        private static ApiResponse<PagedResponse<HomepageItemResponse>>? TryParseSortBy(
            string? value, out HomepageSortBy sortBy)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "mostLikes" : value.Trim();

            switch (normalized)
            {
                case "latest":
                    sortBy = HomepageSortBy.Latest;
                    return null;
                case "mostLikes":
                    sortBy = HomepageSortBy.MostLikes;
                    return null;
                case "mostComments":
                    sortBy = HomepageSortBy.MostComments;
                    return null;
                default:
                    sortBy = HomepageSortBy.MostLikes;
                    return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                        "不支援的排序方式", "INVALID_SORT_BY");
            }
        }

        // validate filter
        private static ApiResponse<PagedResponse<HomepageItemResponse>>? TryParseFilter(
            string? value, out HomepageFilter filter)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "all" : value.Trim();

            switch (normalized)
            {
                case "all":
                    filter = HomepageFilter.All;
                    return null;
                case "postShare":
                    filter = HomepageFilter.PostShare;
                    return null;
                case "postAsk":
                    filter = HomepageFilter.PostAsk;
                    return null;
                case "commission":
                    filter = HomepageFilter.Commission;
                    return null;
                default:
                    filter = HomepageFilter.All;
                    return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                        "不支援的篩選方式", "INVALID_FILTER");
            }
        }

        // turn Content => Content Preview
        private const int HomepageContentPreviewMaxLength = 80;

        private static string BuildContentPreview(string? content)
        {
            var normalized = NormalizedPreviewText(content);

            if(normalized.Length <= HomepageContentPreviewMaxLength)
            {
                return normalized;
            }

            return normalized[..HomepageContentPreviewMaxLength];
        }

        private static string NormalizedPreviewText(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            // Join(" ",...)
            // 把切開後的片段用單一半形空白接回來
            // Split((char[]?)null, ...)
            // 用預設 whitespace 字元分割字串。它會把空白、換行、tab 等 whitespace 當成分隔符
            return string.Join(" ", content.Trim().Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries));
        }

        // Homepage Item Filter: build item => items to list

        // homepage item to list: BuildCommissionHomepageItemAsync, BuildPostHomepageItemAsync

        private async Task<List<HomepageItemResponse>> BuildCommissionHomepageItemsAsync(
            int? currentUserId, CancellationToken cancellationToken)
        {
            var commissions = await _dbContext.Commissions
                .AsNoTracking().AsSplitQuery()
                .Where(c => c.Status != CommissionStatus.Closed && c.ClosedAt == null)
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.CommissionLikes)
                .Include(c => c.CommissionFavorites)
                .Include(c => c.Comments).ToListAsync(cancellationToken);

            return commissions.Select(commission => BuildCommissionHomepageItem(commission, currentUserId)).ToList();
        }

        private async Task<List<HomepageItemResponse>> BuildPostHomepageItemsAsync(
            int? currentUserId, PostType postType, CancellationToken cancellationToken)
        {
            var posts = await _dbContext.Posts.AsNoTracking().AsSplitQuery()
                .Where(p => p.DeletedAt == null && p.PostType == postType)
                .Include(p => p.User).ThenInclude(u => u.AvatarImage)
                .Include(p => p.Images).ThenInclude(i => i.FashionMetadata)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes)
                .Include(p => p.PostFavorites)
                .Include(p => p.Comments)
                .ToListAsync(cancellationToken);

            return posts.Select(post => BuildPostHomepageItem(post, currentUserId)).ToList();
        }

        // Build homepage item: BuildCommissionHomepageItem, BuildPostHomepageItem
        private HomepageItemResponse BuildCommissionHomepageItem(Commission commission, int? currentUserId)
        {
            return new HomepageItemResponse
            {
                ItemType = HomepageItemType.Commission,
                ItemId = commission.Id,
                Author = _userSummaryResponseBuilder.Build(commission.User),
                Title = commission.Title,
                ContentPreview = BuildContentPreview(commission.Content),
                CreatedAt = commission.CreatedAt,
                UpdatedAt = commission.UpdatedAt,
                LikeCount = commission.CommissionLikes.Count,
                IsLiked = currentUserId.HasValue ? commission.CommissionLikes.Any(l => l.UserId == currentUserId.Value) : null,
                FavoriteCount = commission.CommissionFavorites.Count,
                IsFavorited = currentUserId.HasValue ? commission.CommissionFavorites.Any(f => f.UserId == currentUserId.Value) : null,
                CommentCount = commission.Comments.Count(c => c.DeletedAt == null),
                Images = _imageResponseBuilder.BuildList(
                    commission.Images.Where(i => i.DeletedAt == null && i.CommissionRepostId == null)
                    .OrderBy(i => i.CreatedAt)),
                Tags = commission.CommissionTags.OrderBy(ct => ct.Tag.Name)
                    .Select(ct => _mapper.Map<TagResponse>(ct.Tag)).ToList(),
                CommissionStatus = commission.Status,
                CommissionPoints = commission.Points,
                ExpiredAt = commission.ExpiredAt,
                PostType = null
            };
        }

        private HomepageItemResponse BuildPostHomepageItem(Post post, int? currentUserId)
        {
            return new HomepageItemResponse
            {
                ItemType = post.PostType == PostType.Share ? HomepageItemType.PostShare : HomepageItemType.PostAsk,
                ItemId = post.Id,
                Author = _userSummaryResponseBuilder.Build(post.User),
                Title = post.Title,
                ContentPreview = BuildContentPreview(post.Content),
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                CommentCount = post.Comments.Count(c => c.DeletedAt == null),
                LikeCount = post.PostLikes.Count,
                IsLiked = currentUserId.HasValue ? post.PostLikes.Any(like => like.UserId == currentUserId.Value) : null,
                FavoriteCount = post.PostFavorites.Count,
                IsFavorited = currentUserId.HasValue ? post.PostFavorites.Any(f => f.UserId == currentUserId.Value) : null,
                Images = _imageResponseBuilder.BuildList(post.Images.Where(i => i.DeletedAt == null).OrderBy(i => i.CreatedAt)),
                Tags = post.PostTags.OrderBy(pt => pt.Tag.Name).Select(pt => _mapper.Map<TagResponse>(pt.Tag)).ToList(),
                PostType = post.PostType,
                CommissionStatus = null,
                CommissionPoints = null,
                ExpiredAt = null
            };
        }

        // Sorting
        private static IEnumerable<HomepageItemResponse> ApplyHomepageSorting(
            IEnumerable<HomepageItemResponse> items, HomepageSortBy sortBy)
        {
            return sortBy switch
            {
                HomepageSortBy.Latest => items.OrderByDescending(item => item.CreatedAt),
                
                HomepageSortBy.MostLikes => items
                    .OrderByDescending(item => item.LikeCount)
                        .ThenByDescending(item => item.UpdatedAt ?? item.CreatedAt),

                HomepageSortBy.MostComments => items
                    .OrderByDescending(item => item.CommentCount)
                        .ThenByDescending(item => item.UpdatedAt ?? item.CreatedAt),

                _ => items.OrderByDescending(item => item.CreatedAt)
            };
        }

        // Build Response
        private async Task<PagedResponse<HomepageItemResponse>> BuildHomepageResponseAsync(
            int? currentUserId, HomepageFilter filter, HomepageSortBy sortBy, int page, int pageSize, CancellationToken cancellationToken)
        {
            var items = new List<HomepageItemResponse>();

            if(filter is HomepageFilter.All or HomepageFilter.Commission)
            {
                items.AddRange(await BuildCommissionHomepageItemsAsync(currentUserId, cancellationToken));
            }
            if(filter is HomepageFilter.All or HomepageFilter.PostShare)
            {
                items.AddRange(await BuildPostHomepageItemsAsync(currentUserId, PostType.Share, cancellationToken));
            }
            if(filter is HomepageFilter.All or HomepageFilter.PostAsk)
            {
                items.AddRange(await BuildPostHomepageItemsAsync(currentUserId, PostType.Question, cancellationToken));
            }

            var sortedItems = ApplyHomepageSorting(items, sortBy).ToList();

            var pagedItems = sortedItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResponse<HomepageItemResponse>
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = sortedItems.Count,
                TotalPages = PagingHelper.CalculateTotalPages(sortedItems.Count, pageSize)
            };
        }

    }
}
